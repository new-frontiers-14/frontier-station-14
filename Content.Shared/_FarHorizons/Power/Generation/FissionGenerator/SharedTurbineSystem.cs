// SPDX-FileCopyrightText: 2025 jhrushbe <capnmerry@gmail.com>
// SPDX-FileCopyrightText: 2025 rottenheadphones <juaelwe@outlook.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: CC-BY-NC-SA-3.0


using Content.Shared.Administration.Logs;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Electrocution;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Repairable;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._FarHorizons.Power.Generation.FissionGenerator;

// Ported and modified from goonstation by Jhrushbe.
// CC-BY-NC-SA-3.0
// https://github.com/goonstation/goonstation/blob/ff86b044/code/obj/nuclearreactor/turbine.dm

public abstract class SharedTurbineSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] protected readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TurbineComponent, ExaminedEvent>(OnExamined);

        SubscribeLocalEvent<TurbineComponent, InteractUsingEvent>(RepairTurbine);
        SubscribeLocalEvent<TurbineComponent, RepairDoAfterEvent>(OnRepairTurbineFinished);
    }

    private void OnExamined(Entity<TurbineComponent> ent, ref ExaminedEvent args)
    {
        var comp = ent.Comp;
        if (!Comp<TransformComponent>(ent).Anchored || !args.IsInDetailsRange) // Not anchored? Out of range? No status.
            return;

        using (args.PushGroup(nameof(TurbineComponent)))
        {
            if(comp.CurrentStator == null)
                args.PushMarkup(Loc.GetString("gas-turbine-examine-stator-null"));

            if (comp.CurrentBlade == null)
                args.PushMarkup(Loc.GetString("gas-turbine-examine-blade-null"));
            else
            {
                switch (comp.RPM)
                {
                    case float n when n is >= 0 and <= 1:
                        args.PushMarkup(Loc.GetString("turbine-spinning-0")); // " The blades are not spinning."
                        break;
                    case float n when n is > 1 and <= 60:
                        args.PushMarkup(Loc.GetString("turbine-spinning-1")); // " The blades are turning slowly."
                        break;
                    case float n when n > 60 && n <= comp.BestRPM * 0.5:
                        args.PushMarkup(Loc.GetString("turbine-spinning-2")); // " The blades are spinning."
                        break;
                    case float n when n > comp.BestRPM * 0.5 && n <= comp.BestRPM * 1.2:
                        args.PushMarkup(Loc.GetString("turbine-spinning-3")); // " The blades are spinning quickly."
                        break;
                    case float n when n > comp.BestRPM * 1.2 && n <= float.PositiveInfinity:
                        args.PushMarkup(Loc.GetString("turbine-spinning-4")); // " The blades are spinning out of control!"
                        break;
                    default:
                        break;
                }
            }

            if (comp.Ruined)
            {
                args.PushMarkup(Loc.GetString("turbine-ruined")); // " It's completely broken!"
            }
            else if (comp.BladeHealth <= 0.25 * comp.BladeHealthMax)
            {
                args.PushMarkup(Loc.GetString("turbine-damaged-3")); // " It's critically damaged!"
            }
            else if (comp.BladeHealth <= 0.5 * comp.BladeHealthMax)
            {
                args.PushMarkup(Loc.GetString("turbine-damaged-2")); // " The turbine looks badly damaged."
            }
            else if (comp.BladeHealth <= 0.75 * comp.BladeHealthMax)
            {
                args.PushMarkup(Loc.GetString("turbine-damaged-1")); // " The turbine looks a bit scuffed."
            }
            else
            {
                args.PushMarkup(Loc.GetString("turbine-damaged-0")); // " It appears to be in good condition."
            }
        }
    }

    protected void UpdateAppearance(EntityUid uid, TurbineComponent? comp = null, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref comp, ref appearance, false))
            return;

        _appearance.SetData(uid, TurbineVisuals.TurbineRuined, comp.Ruined);

        _appearance.SetData(uid, TurbineVisuals.DamageSpark, comp.IsSparking);
        _appearance.SetData(uid, TurbineVisuals.DamageSmoke, comp.IsSmoking);
    }

    protected void PlayAudio(SoundSpecifier? sound, EntityUid uid, out EntityUid? audioStream, AudioParams? audioParams = null)
    {
        if (sound == null || audioParams == null)
        {
            audioStream = null;
            return;
        }

        var loop = audioParams.Value.WithLoop(true);
        var stream = false
            ? _audio.PlayPredicted(sound, uid, uid, loop)
            : _audio.PlayPvs(sound, uid, loop);
        audioStream = stream?.Entity is { } entity ? entity : null;
    }

    protected static bool AdjustStatorLoad(TurbineComponent turbine, float change)
    {
        var newSet = Math.Max(turbine.StatorLoad + change, 1000f);
        if (turbine.StatorLoad != newSet)
        {
            turbine.StatorLoad = newSet;
            return true;
        }
        return false;
    }

    #region Repairs
    private void RepairTurbine(EntityUid uid, TurbineComponent comp, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if(_toolSystem.HasQuality(args.Used, comp.RepairTool))
        {
            if (comp.CurrentBlade == null)
            {
                _popupSystem.PopupEntity(Loc.GetString("gas-turbine-repair-fail-blade"), args.User, args.User, PopupType.Medium);
                args.Handled = true;
                return;
            }

            if (comp.CurrentStator == null)
            {
                _popupSystem.PopupEntity(Loc.GetString("gas-turbine-repair-fail-stator"), args.User, args.User, PopupType.Medium);
                args.Handled = true;
                return;
            }

            if (comp.BladeHealth >= comp.BladeHealthMax && !comp.Ruined)
                return;

            args.Handled = _toolSystem.UseTool(args.Used, args.User, uid, comp.RepairDelay, comp.RepairTool, new RepairDoAfterEvent(), comp.RepairFuelCost);
        }
    }

    //Gotta love server/client desync
    protected virtual void OnRepairTurbineFinished(EntityUid uid, TurbineComponent comp, ref RepairDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (comp.Ruined)
        {
            comp.Ruined = false;
            if (comp.BladeHealth <= 0) { comp.BladeHealth = 1; }
            UpdateHealthIndicators(uid, comp);
        }
        else if (comp.BladeHealth < comp.BladeHealthMax)
        {
            comp.BladeHealth++;
            UpdateHealthIndicators(uid, comp);
        }
        else if (comp.BladeHealth >= comp.BladeHealthMax)
        {
            // This should technically never occur, but just in case...
        }

        if (!_entityManager.TryGetComponent<DamageableComponent>(uid, out var damageableComponent))
            return;

        _damageableSystem.SetAllDamage(uid, damageableComponent, 0);
    }

    protected void UpdateHealthIndicators(EntityUid uid, TurbineComponent comp)
    {
        if (comp.BladeHealth <= 0.75 * comp.BladeHealthMax && !comp.IsSparking)
        {
            comp.IsSparking = true;
            _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/PowerSink/electric.ogg"), uid, AudioParams.Default.WithPitchScale(0.75f));
            _popupSystem.PopupEntity(Loc.GetString("turbine-spark", ("owner", uid)), uid, PopupType.MediumCaution);
        }
        else if (comp.BladeHealth > 0.75 * comp.BladeHealthMax && comp.IsSparking)
        {
            comp.IsSparking = false;
            _popupSystem.PopupEntity(Loc.GetString("turbine-spark-stop", ("owner", uid)), uid, PopupType.Medium);
        }

        if (comp.BladeHealth <= 0.5 * comp.BladeHealthMax && !comp.IsSmoking)
        {
            comp.IsSmoking = true;
            _popupSystem.PopupEntity(Loc.GetString("turbine-smoke", ("owner", uid)), uid, PopupType.MediumCaution);
        }
        else if (comp.BladeHealth > 0.5 * comp.BladeHealthMax && comp.IsSmoking)
        {
            comp.IsSmoking = false;
            _popupSystem.PopupEntity(Loc.GetString("turbine-smoke-stop", ("owner", uid)), uid, PopupType.Medium);
        }

        _entityManager.EnsureComponent<ElectrifiedComponent>(uid).Enabled = comp.IsSparking;

        UpdateAppearance(uid, comp);
    }

    #endregion
}

[Serializable, NetSerializable]
public sealed partial class RepairDoAfterEvent : SimpleDoAfterEvent
{
}
