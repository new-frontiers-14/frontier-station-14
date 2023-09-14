using Content.Shared.Gravity;
using Content.Shared.Hands.Components;
using Robust.Shared.Utility;

namespace Content.Shared.DoAfter;

public abstract partial class SharedDoAfterSystem : EntitySystem
{
    [Dependency] private readonly IDynamicTypeFactory _factory = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = GameTiming.CurTime;
        var xformQuery = GetEntityQuery<TransformComponent>();
        var handsQuery = GetEntityQuery<HandsComponent>();

        var enumerator = EntityQueryEnumerator<ActiveDoAfterComponent, DoAfterComponent>();
        while (enumerator.MoveNext(out var uid, out var active, out var comp))
        {
            Update(uid, active, comp, time, xformQuery, handsQuery);
        }
    }

    protected void Update(
        EntityUid uid,
        ActiveDoAfterComponent active,
        DoAfterComponent comp,
        TimeSpan time,
        EntityQuery<TransformComponent> xformQuery,
        EntityQuery<HandsComponent> handsQuery)
    {
        var dirty = false;

        foreach (var doAfter in comp.DoAfters.Values)
        {
            if (doAfter.CancelledTime != null)
            {
                if (time - doAfter.CancelledTime.Value > ExcessTime)
                {
                    comp.DoAfters.Remove(doAfter.Index);
                    dirty = true;
                }
                continue;
            }

            if (doAfter.Completed)
            {
                if (time - doAfter.StartTime > doAfter.Args.Delay + ExcessTime)
                {
                    comp.DoAfters.Remove(doAfter.Index);
                    dirty = true;
                }
                continue;
            }

            if (ShouldCancel(doAfter, xformQuery, handsQuery))
            {
                InternalCancel(doAfter, comp);
                dirty = true;
                continue;
            }

            if (time - doAfter.StartTime >= doAfter.Args.Delay)
            {
                TryComplete(doAfter, comp);
                dirty = true;
            }
        }

        if (dirty)
            Dirty(uid, comp);

        if (comp.DoAfters.Count == 0)
            RemCompDeferred(uid, active);
    }

    private bool TryAttemptEvent(DoAfter doAfter)
    {
        var args = doAfter.Args;

        if (args.ExtraCheck?.Invoke() == false)
            return false;

        if (doAfter.AttemptEvent == null)
        {
            // I feel like this is somewhat cursed, but its the only way I can think of without having to just send
            // redundant data over the network and increasing DoAfter boilerplate.
            var evType = typeof(DoAfterAttemptEvent<>).MakeGenericType(args.Event.GetType());
            doAfter.AttemptEvent = _factory.CreateInstance(evType, new object[] { doAfter, args.Event });
        }

        if (args.EventTarget != null)
            RaiseLocalEvent(args.EventTarget.Value, doAfter.AttemptEvent, args.Broadcast);
        else
            RaiseLocalEvent(doAfter.AttemptEvent);

        var ev = (CancellableEntityEventArgs) doAfter.AttemptEvent;
        if (!ev.Cancelled)
            return true;

        ev.Uncancel();
        return false;
    }

    private void TryComplete(DoAfter doAfter, DoAfterComponent component)
    {
        if (doAfter.Cancelled || doAfter.Completed)
            return;

        // Perform final check (if required)
        if (doAfter.Args.AttemptFrequency == AttemptFrequency.StartAndEnd
            && !TryAttemptEvent(doAfter))
        {
            InternalCancel(doAfter, component);
            return;
        }

        doAfter.Completed = true;

        RaiseDoAfterEvents(doAfter, component);

        if (doAfter.Args.Event.Repeat)
        {
            doAfter.StartTime = GameTiming.CurTime;
            doAfter.Completed = false;
        }
    }

    private bool ShouldCancel(DoAfter doAfter,
        EntityQuery<TransformComponent> xformQuery,
        EntityQuery<HandsComponent> handsQuery)
    {
        var args = doAfter.Args;

        //re-using xformQuery for Exists() checks.
        if (args.Used is { } used && !xformQuery.HasComponent(used))
            return true;

        if (args.EventTarget is {Valid: true} eventTarget && !xformQuery.HasComponent(eventTarget))
            return true;

        if (!xformQuery.TryGetComponent(args.User, out var userXform))
            return true;

        TransformComponent? targetXform = null;
        if (args.Target is { } target && !xformQuery.TryGetComponent(target, out targetXform))
            return true;

        TransformComponent? usedXform = null;
        if (args.Used is { } @using && !xformQuery.TryGetComponent(@using, out usedXform))
            return true;

        // TODO: Re-use existing xform query for these calculations.
        // when there is no gravity you will be drifting 99% of the time making many doafters impossible
        // so this just ignores your movement if you are weightless (unless the doafter sets BreakOnWeightlessMove then moving will still break it)
        if (args.BreakOnUserMove
            && !userXform.Coordinates.InRange(EntityManager, _transform, doAfter.UserPosition, args.MovementThreshold)
            && (args.BreakOnWeightlessMove || !_gravity.IsWeightless(args.User, xform: userXform)))
            return true;

        if (args.BreakOnTargetMove)
        {
            DebugTools.Assert(targetXform != null, "Break on move is true, but no target specified?");
            if (targetXform != null && targetXform.Coordinates.TryDistance(EntityManager, userXform.Coordinates, out var distance))
            {
                // once the target moves too far from you the do after breaks
                if (Math.Abs(distance - doAfter.TargetDistance) > args.MovementThreshold)
                    return true;
            }
        }

        if (args.AttemptFrequency == AttemptFrequency.EveryTick && !TryAttemptEvent(doAfter))
            return true;

        if (args.NeedHand)
        {
            if (!handsQuery.TryGetComponent(args.User, out var hands) || hands.Count == 0)
                return true;

            if (args.BreakOnHandChange && (hands.ActiveHand?.Name != doAfter.InitialHand
                                           || hands.ActiveHandEntity != doAfter.InitialItem))
            {
                return true;
            }
        }

        if (args.RequireCanInteract && !_actionBlocker.CanInteract(args.User, args.Target))
            return true;

        if (args.DistanceThreshold != null)
        {
            if (targetXform != null
                && !args.User.Equals(args.Target)
                && !userXform.Coordinates.InRange(EntityManager, _transform, targetXform.Coordinates,
                    args.DistanceThreshold.Value))
            {
                return true;
            }

            if (usedXform != null
                && !userXform.Coordinates.InRange(EntityManager, _transform, usedXform.Coordinates,
                    args.DistanceThreshold.Value))
            {
                return true;
            }
        }

        return false;
    }
}
