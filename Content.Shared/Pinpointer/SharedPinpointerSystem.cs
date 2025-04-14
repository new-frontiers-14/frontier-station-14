using Content.Shared._NF.Pinpointer;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Pinpointer;

public abstract class SharedPinpointerSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!; // Frontier

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PinpointerComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<PinpointerComponent, GotUnEmaggedEvent>(OnUnemagged); // Frontier
        SubscribeLocalEvent<PinpointerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<PinpointerComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<PinpointerComponent, PinpointerDoAfterEvent>(OnPinpointerDoAfter); // Frontier
    }

    /// <summary>
    ///     Set the target if capable
    /// </summary>
    private void OnAfterInteract(EntityUid uid, PinpointerComponent component, AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target is not { } target)
            return;

        if (!component.CanRetarget || component.IsActive)
            return;

        // Frontier: disallow pinpointing mobs
        if (!component.CanTargetMobs && HasComp<MobStateComponent>(args.Target))
            return;

        // TODO add doafter once the freeze is lifted
        args.Handled = true;

        // Frontier: the below was made into a do-after, see OnPinpointerDoAfter.
        // component.Target = args.Target;
        // _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.User):player} set target of {ToPrettyString(uid):pinpointer} to {ToPrettyString(component.Target.Value):target}");
        // if (component.UpdateTargetName)
        //     component.TargetName = component.Target == null ? null : Identity.Name(component.Target.Value, EntityManager);

        var daArgs = new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(component.RetargetDoAfter),
            new PinpointerDoAfterEvent(), uid, args.Target, uid)
        {
            BreakOnDamage = true,
            BreakOnWeightlessMove = true,
            CancelDuplicate = true,
            BreakOnHandChange = true,
            NeedHand = true,
            BreakOnMove = true,
        };
        _doAfter.TryStartDoAfter(daArgs);
        // End Frontier
    }

    private void OnPinpointerDoAfter(EntityUid uid, PinpointerComponent component, PinpointerDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        // Frontier: two-way pinpointer tracking
        if (component.SetsTarget)
        {
            if (TryComp<PinpointerTargetComponent>(component.Target, out var pinpointerTarget))
            {
                pinpointerTarget.Entities.Remove(uid);
                if (pinpointerTarget.Entities.Count <= 0)
                    RemComp<PinpointerTargetComponent>(component.Target.Value);
            }
            if (args.Target != null)
            {
                pinpointerTarget = EnsureComp<PinpointerTargetComponent>(args.Target.Value);
                pinpointerTarget.Entities.Add(uid);
            }
        }
        // End Frontier: two-way pinpointer tracking

        component.Target = args.Target;
        _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.User):player} set target of {ToPrettyString(uid):pinpointer} to {ToPrettyString(component.Target):target}");
        if (component.UpdateTargetName)
            component.TargetName = component.Target == null ? null : Identity.Name(component.Target.Value, EntityManager);
    }

    /// <summary>
    ///     Set pinpointers target to track
    /// </summary>
    public virtual void SetTarget(EntityUid uid, EntityUid? target, PinpointerComponent? pinpointer = null)
    {
        if (!Resolve(uid, ref pinpointer))
            return;

        if (pinpointer.Target == target)
            return;

        // Frontier: two-way pinpointer tracking
        if (pinpointer.SetsTarget)
        {
            if (TryComp<PinpointerTargetComponent>(pinpointer.Target, out var pinpointerTarget))
            {
                pinpointerTarget.Entities.Remove(uid);
                if (pinpointerTarget.Entities.Count <= 0)
                    RemComp<PinpointerTargetComponent>(pinpointer.Target.Value);
            }
            if (target != null)
            {
                pinpointerTarget = EnsureComp<PinpointerTargetComponent>(target.Value);
                pinpointerTarget.Entities.Add(uid);
            }
        }
        // End Frontier: two-way pinpointer tracking

        pinpointer.Target = target;
        if (pinpointer.UpdateTargetName)
            pinpointer.TargetName = target == null ? null : Identity.Name(target.Value, EntityManager);
        if (pinpointer.IsActive)
            UpdateDirectionToTarget(uid, pinpointer);
    }

    /// <summary>
    ///     Update direction from pinpointer to selected target (if it was set)
    /// </summary>
    protected virtual void UpdateDirectionToTarget(EntityUid uid, PinpointerComponent? pinpointer = null)
    {

    }

    private void OnExamined(EntityUid uid, PinpointerComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || component.TargetName == null)
            return;

        args.PushMarkup(Loc.GetString("examine-pinpointer-linked", ("target", component.TargetName)));
    }

    /// <summary>
    ///     Manually set distance from pinpointer to target
    /// </summary>
    public void SetDistance(EntityUid uid, Distance distance, PinpointerComponent? pinpointer = null)
    {
        if (!Resolve(uid, ref pinpointer))
            return;

        if (distance == pinpointer.DistanceToTarget)
            return;

        pinpointer.DistanceToTarget = distance;
        Dirty(uid, pinpointer);
    }

    /// <summary>
    ///     Try to manually set pinpointer arrow direction.
    ///     If difference between current angle and new angle is smaller than
    ///     pinpointer precision, new value will be ignored and it will return false.
    /// </summary>
    public bool TrySetArrowAngle(EntityUid uid, Angle arrowAngle, PinpointerComponent? pinpointer = null)
    {
        if (!Resolve(uid, ref pinpointer))
            return false;

        if (pinpointer.ArrowAngle.EqualsApprox(arrowAngle, pinpointer.Precision))
            return false;

        pinpointer.ArrowAngle = arrowAngle;
        Dirty(uid, pinpointer);

        return true;
    }

    /// <summary>
    ///     Activate/deactivate pinpointer screen. If it has target it will start tracking it.
    /// </summary>
    public void SetActive(EntityUid uid, bool isActive, PinpointerComponent? pinpointer = null)
    {
        if (!Resolve(uid, ref pinpointer))
            return;
        if (isActive == pinpointer.IsActive)
            return;

        pinpointer.IsActive = isActive;
        Dirty(uid, pinpointer);
    }


    /// <summary>
    ///     Toggle Pinpointer screen. If it has target it will start tracking it.
    /// </summary>
    /// <returns>True if pinpointer was activated, false otherwise</returns>
    public virtual bool TogglePinpointer(EntityUid uid, PinpointerComponent? pinpointer = null)
    {
        if (!Resolve(uid, ref pinpointer))
            return false;

        var isActive = !pinpointer.IsActive;
        SetActive(uid, isActive, pinpointer);
        return isActive;
    }

    private void OnEmagged(EntityUid uid, PinpointerComponent component, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(uid, EmagType.Interaction))
            return;

        if (component.CanRetarget)
            return;

        args.Handled = true;
        component.CanRetarget = true;
    }

    // Frontier: demag
    private void OnUnemagged(EntityUid uid, PinpointerComponent component, ref GotUnEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (!_emag.CheckFlag(uid, EmagType.Interaction))
            return;

        if (component.CanRetarget)
            component.CanRetarget = false;

        args.Handled = true;
    }
    // End Frontier: demag
}

// Frontier - do-after
[Serializable, NetSerializable]
public sealed partial class PinpointerDoAfterEvent : SimpleDoAfterEvent
{
}
