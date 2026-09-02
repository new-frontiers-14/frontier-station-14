using Content.Server.Power.EntitySystems;
using Content.Server.Radio;
using Content.Server.Station.Components;
using Content.Server.SurveillanceCamera;
using Content.Shared.Emp;
using Robust.Shared.Map;
using Content.Server.Examine; // Frontier: examine verb
using Content.Server.Power.Components; // Frontier
using Content.Shared.Tiles; // Frontier
using Content.Shared.Trigger.Components.Effects; // Frontier
using Content.Shared.Verbs; // Frontier: examine verb
using Content.Shared._NF.Emp.Components; // Frontier
using Robust.Server.GameStates; // Frontier: EMP Blast PVS
using Robust.Shared.Configuration; // Frontier: EMP Blast PVS
using Robust.Shared.Utility; // Frontier: examine verb
using Robust.Shared; // Frontier: EMP Blast PVS

namespace Content.Server.Emp;

public sealed class EmpSystem : SharedEmpSystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly PvsOverrideSystem _pvs = default!; // Frontier: EMP Blast PVS
    [Dependency] private readonly IConfigurationManager _cfg = default!; // Frontier: EMP Blast PVS
    [Dependency] private readonly ExamineSystem _examine = default!; // Frontier: examine verb

    public const string EmpPulseEffectPrototype = "EffectEmpBlast"; // Frontier: EffectEmpPulse

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EmpOnTriggerComponent, GetVerbsEvent<ExamineVerb>>(OnEmpTriggerExamine); // Frontier
        SubscribeLocalEvent<EmpDescriptionComponent, GetVerbsEvent<ExamineVerb>>(OnEmpDescriptorExamine); // Frontier

        SubscribeLocalEvent<EmpDisabledComponent, RadioSendAttemptEvent>(OnRadioSendAttempt);
        SubscribeLocalEvent<EmpDisabledComponent, RadioReceiveAttemptEvent>(OnRadioReceiveAttempt);
        //SubscribeLocalEvent<EmpDisabledComponent, ApcToggleMainBreakerAttemptEvent>(OnApcToggleMainBreaker); // Frontier: Upstream - #28984
        //SubscribeLocalEvent<EmpDisabledComponent, SurveillanceCameraSetActiveAttemptEvent>(OnCameraSetActive); // Frontier: Upstream - #28984
    }

    public override void EmpPulse(MapCoordinates coordinates, float range, float energyConsumption, float duration, List<EntityUid>? immuneGrids = null) // Frontier: Add immuneGrids
    {
        foreach (var uid in _lookup.GetEntitiesInRange(coordinates, range))
        {
            // Frontier: Block EMP on grid
            var gridUid = Transform(uid).GridUid;
            if (gridUid != null &&
                (immuneGrids != null && immuneGrids.Contains(gridUid.Value) ||
                TryComp<ProtectedGridComponent>(gridUid, out var prot) && prot.PreventEmpEvents))
                continue;
            // End Frontier: block EMP on grid

            TryEmpEffects(uid, energyConsumption, duration);
        }

        var empBlast = Spawn(EmpPulseEffectPrototype, coordinates); // Frontier: Added visual effect
        EnsureComp<EmpBlastComponent>(empBlast, out var empBlastComp); // Frontier
        empBlastComp.VisualRange = range; // Frontier

        if (range > _cfg.GetCVar(CVars.NetMaxUpdateRange)) // Frontier
            _pvs.AddGlobalOverride(empBlast); // Frontier

        Dirty(empBlast, empBlastComp); // Frontier
    }

    /// <summary>
    ///   Triggers an EMP pulse at the given location, by first raising an <see cref="EmpAttemptEvent"/>, then a raising <see cref="EmpPulseEvent"/> on all entities in range.
    /// </summary>
    /// <param name="coordinates">The location to trigger the EMP pulse at.</param>
    /// <param name="range">The range of the EMP pulse.</param>
    /// <param name="energyConsumption">The amount of energy consumed by the EMP pulse.</param>
    /// <param name="duration">The duration of the EMP effects.</param>
    public void EmpPulse(EntityCoordinates coordinates, float range, float energyConsumption, float duration)
    {
        foreach (var uid in _lookup.GetEntitiesInRange(coordinates, range))
        {
            TryEmpEffects(uid, energyConsumption, duration);
        }
        Spawn(EmpPulseEffectPrototype, coordinates);
    }

    /// <summary>
    ///    Attempts to apply the effects of an EMP pulse onto an entity by first raising an <see cref="EmpAttemptEvent"/>, followed by raising a <see cref="EmpPulseEvent"/> on it.
    /// </summary>
    /// <param name="uid">The entity to apply the EMP effects on.</param>
    /// <param name="energyConsumption">The amount of energy consumed by the EMP.</param>
    /// <param name="duration">The duration of the EMP effects.</param>
    public void TryEmpEffects(EntityUid uid, float energyConsumption, float duration)
    {
        var attemptEv = new EmpAttemptEvent();
        RaiseLocalEvent(uid, attemptEv);
        if (attemptEv.Cancelled)
            return;

        DoEmpEffects(uid, energyConsumption, duration);
    }

    /// <summary>
    ///    Applies the effects of an EMP pulse onto an entity by raising a <see cref="EmpPulseEvent"/> on it.
    /// </summary>
    /// <param name="uid">The entity to apply the EMP effects on.</param>
    /// <param name="energyConsumption">The amount of energy consumed by the EMP.</param>
    /// <param name="duration">The duration of the EMP effects.</param>
    public void DoEmpEffects(EntityUid uid, float energyConsumption, float duration)
    {
        var ev = new EmpPulseEvent(energyConsumption, false, false, TimeSpan.FromSeconds(duration));
        RaiseLocalEvent(uid, ref ev);

        if (ev.Affected)
            Spawn(EmpDisabledEffectPrototype, Transform(uid).Coordinates);

        if (ev.Disabled)
        {
            // Frontier: Upstream - #28984 start
            //disabled.DisabledUntil = Timing.CurTime + TimeSpan.FromSeconds(duration);
            var disabled = EnsureComp<EmpDisabledComponent>(uid);
            if (disabled.DisabledUntil == TimeSpan.Zero)
            {
                disabled.DisabledUntil = Timing.CurTime;
            }
            disabled.DisabledUntil = disabled.DisabledUntil + TimeSpan.FromSeconds(duration);

            /// i tried my best to go through the Pow3r server code but i literally couldn't find in relation to PowerNetworkBatteryComponent that uses the event system
            /// the code is otherwise too esoteric for my innocent eyes
            if (TryComp<PowerNetworkBatteryComponent>(uid, out var powerNetBattery))
            {
                powerNetBattery.CanCharge = false;
            }
            // Frontier: Upstream - #28984 end
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<EmpDisabledComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.DisabledUntil < Timing.CurTime)
            {
                RemComp<EmpDisabledComponent>(uid);
                var ev = new EmpDisabledRemoved();
                RaiseLocalEvent(uid, ref ev);

                if (TryComp<PowerNetworkBatteryComponent>(uid, out var powerNetBattery)) // Frontier: Upstream - #28984
                {
                    powerNetBattery.CanCharge = true;
                }
            }
        }
    }

    // Frontier: examine EMP trigger objects
    private void OnEmpTriggerExamine(EntityUid uid, EmpOnTriggerComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var msg = GetEmpDescription(component.Range, component.EnergyConsumption, (float)component.DisableDuration.TotalSeconds);

        _examine.AddDetailedExamineVerb(args, component, msg,
            Loc.GetString("emp-examinable-verb-text"), "/Textures/Interface/VerbIcons/smite.svg.192dpi.png",
            Loc.GetString("emp-examinable-verb-message"));
    }
    private void OnEmpDescriptorExamine(EntityUid uid, EmpDescriptionComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var msg = GetEmpDescription(component.Range, component.EnergyConsumption, component.DisableDuration);

        _examine.AddDetailedExamineVerb(args, component, msg,
            Loc.GetString("emp-examinable-verb-text"), "/Textures/Interface/VerbIcons/smite.svg.192dpi.png",
            Loc.GetString("emp-examinable-verb-message"));
    }

    private FormattedMessage GetEmpDescription(float range, float energy, float time)
    {
        var msg = new FormattedMessage();
        msg.AddMarkupOrThrow(Loc.GetString("emp-examine"));
        msg.PushNewline();
        msg.AddMarkupOrThrow(Loc.GetString("emp-range-value",
            ("value", range)));
        msg.PushNewline();
        msg.AddMarkupOrThrow(Loc.GetString("emp-energy-value",
            ("value", energy)));
        msg.PushNewline();
        msg.AddMarkupOrThrow(Loc.GetString("emp-time-value",
            ("value", time)));
        return msg;
    }
    // End Frontier

    private void OnRadioSendAttempt(EntityUid uid, EmpDisabledComponent component, ref RadioSendAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnRadioReceiveAttempt(EntityUid uid, EmpDisabledComponent component, ref RadioReceiveAttemptEvent args)
    {
        args.Cancelled = true;
    }

    //private void OnApcToggleMainBreaker(EntityUid uid, EmpDisabledComponent component, ref ApcToggleMainBreakerAttemptEvent args) // Frontier: Upstream - #28984
    //{
    //    args.Cancelled = true;
    //}

    //private void OnCameraSetActive(EntityUid uid, EmpDisabledComponent component, ref SurveillanceCameraSetActiveAttemptEvent args) // Frontier: Upstream - #28984
    //{
    //    args.Cancelled = true;
    //}

}

/// <summary>
/// Raised on an entity before <see cref="EmpPulseEvent"/>. Cancel this to prevent the emp event being raised.
/// </summary>
public sealed partial class EmpAttemptEvent : CancellableEntityEventArgs;

[ByRefEvent]
public record struct EmpPulseEvent(float EnergyConsumption, bool Affected, bool Disabled, TimeSpan Duration);

[ByRefEvent]
public record struct EmpDisabledRemoved();
