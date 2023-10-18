using Content.Shared.Prying.Components;
using Content.Shared.Verbs;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Doors.Components;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Interaction;
using PryUnpoweredComponent = Content.Shared.Prying.Components.PryUnpoweredComponent;

namespace Content.Shared.Prying.Systems;

/// <summary>
/// Handles prying of entities (e.g. doors)
/// </summary>
public sealed class PryingSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Mob prying doors
        SubscribeLocalEvent<DoorComponent, GetVerbsEvent<AlternativeVerb>>(OnDoorAltVerb);
        SubscribeLocalEvent<DoorComponent, DoorPryDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<DoorComponent, InteractUsingEvent>(TryPryDoor);
    }

    private void TryPryDoor(EntityUid uid, DoorComponent comp, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryPry(uid, args.User, out _, args.Used);
    }

    private void OnDoorAltVerb(EntityUid uid, DoorComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!TryComp<PryingComponent>(args.User, out var tool))
            return;

        args.Verbs.Add(new AlternativeVerb()
        {
            Text = Loc.GetString("door-pry"),
            Impact = LogImpact.Low,
            Act = () => TryPry(uid, args.User, out _, args.User),
        });
    }

    /// <summary>
    /// Attempt to pry an entity.
    /// </summary>
    public bool TryPry(EntityUid target, EntityUid user, out DoAfterId? id, EntityUid tool)
    {
        id = null;

        PryingComponent? comp = null;
        if (!Resolve(tool, ref comp, false))
            return false;

        if (!comp.Enabled)
            return false;

        if (!CanPry(target, user, comp))
        {
            // If we have reached this point we want the event that caused this
            // to be marked as handled as a popup would be generated on failure.
            return true;
        }

        StartPry(target, user, tool, comp.SpeedModifier, out id);

        return true;
    }

    /// <summary>
    /// Try to pry an entity.
    /// </summary>
    public bool TryPry(EntityUid target, EntityUid user, out DoAfterId? id)
    {
        id = null;

        if (!CanPry(target, user))
            // If we have reached this point we want the event that caused this
            // to be marked as handled as a popup would be generated on failure.
            return true;

        return StartPry(target, user, null, 0.1f, out id); // hand-prying is much slower
    }

    private bool CanPry(EntityUid target, EntityUid user, PryingComponent? comp = null)
    {
        BeforePryEvent canev;

        if (comp != null)
        {
            canev = new BeforePryEvent(user, comp.PryPowered, comp.Force);
        }
        else
        {
            if (!TryComp<PryUnpoweredComponent>(target, out _))
                return false;
            canev = new BeforePryEvent(user, false, false);
        }

        RaiseLocalEvent(target, ref canev);

        if (canev.Cancelled)
            return false;
        return true;
    }

    private bool StartPry(EntityUid target, EntityUid user, EntityUid? tool, float toolModifier, [NotNullWhen(true)] out DoAfterId? id)
    {
        var modEv = new GetPryTimeModifierEvent(user);

        RaiseLocalEvent(target, ref modEv);
        var doAfterArgs = new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(modEv.BaseTime * modEv.PryTimeModifier / toolModifier), new DoorPryDoAfterEvent(), target, target, tool)
        {
            BreakOnDamage = true,
            BreakOnUserMove = true,
        };

        if (tool != null)
        {
            _adminLog.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(user)} is using {ToPrettyString(tool.Value)} to pry {ToPrettyString(target)}");
        }
        else
        {
            _adminLog.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(user)} is prying {ToPrettyString(target)}");
        }
        return _doAfterSystem.TryStartDoAfter(doAfterArgs, out id);
    }

    private void OnDoAfter(EntityUid uid, DoorComponent door, DoorPryDoAfterEvent args)
    {
        if (args.Cancelled)
            return;
        if (args.Target is null)
            return;

        PryingComponent? comp = null;

        if (args.Used != null && Resolve(args.Used.Value, ref comp))
            _audioSystem.PlayPredicted(comp.UseSound, args.Used.Value, args.User);

        var ev = new PriedEvent(args.User);
        RaiseLocalEvent(uid, ref ev);
    }
}

[Serializable, NetSerializable]
public sealed partial class DoorPryDoAfterEvent : SimpleDoAfterEvent
{
}
