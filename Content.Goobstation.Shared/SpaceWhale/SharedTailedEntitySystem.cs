// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Numerics;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared._Goobstation.SpaceWhale.Weapons;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Physics.Events;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._Goobstation.SpaceWhale;

public abstract class SharedTailedEntitySystem : EntitySystem
{
    [Dependency] protected readonly SharedTransformSystem TransformSystem = default!;

    [Dependency] protected readonly EntityQuery<TailedEntitySegmentComponent> SegmentQuery = default!;

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly EntityLookupSystem _look = default!;

    private readonly HashSet<Entity<TailedEntitySegmentComponent>> _lookSegments = new();

    public override void Initialize()
    {
        base.Initialize();

        UpdatesOutsidePrediction = true;

        SubscribeLocalEvent<TailedEntityComponent, GetLightAttackRangeEvent>(OnGetRange);
        SubscribeLocalEvent<TailedEntityComponent, MeleeInRangeEvent>(OnInRange);
        SubscribeLocalEvent<TailedEntityComponent, AttackAttemptEvent>(OnAttackAttempt);
        SubscribeLocalEvent<TailedEntityComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<TailedEntityComponent, TailedEntityForceContractEvent>(OnForceContract);
    }

    private void OnForceContract(Entity<TailedEntityComponent> ent, ref TailedEntityForceContractEvent args)
    {
        args.Handled = true;
        ent.Comp.PreventSegmentCollide = true;

        var pos = TransformSystem.GetMapCoordinates(ent);

        foreach (var data in ent.Comp.TailSegments)
        {
            if (!TryGetEntity(data.Segment, out var segment) || !SegmentQuery.TryComp(segment.Value, out var comp))
                continue;

            comp.Coords = pos;
            TransformSystem.SetMapCoordinates(segment.Value, pos);
            Dirty(segment.Value, comp);

            data.WorldPosition = pos.Position;
        }

        Dirty(ent);
    }

    private void OnPreventCollide(Entity<TailedEntityComponent> ent, ref PreventCollideEvent args)
    {
        var other = args.OtherEntity;

        var index = ent.Comp.TailSegments.FindIndex(x => TryGetEntity(x.Segment, out var e) && e == other);

        if (index < 0)
            return;

        if (ent.Comp.PreventSegmentCollide ||
            ent.Comp.PreventFirstSegmentsCollideAmount < 1 && index < ent.Comp.PreventFirstSegmentsCollideAmount)
            args.Cancelled = true;
    }

    private void OnAttackAttempt(Entity<TailedEntityComponent> ent, ref AttackAttemptEvent args)
    {
        if (args.Target is { } target &&
            ent.Comp.TailSegments.Any(x => TryGetEntity(x.Segment, out var e) && e == target))
            args.Cancel();
    }

    private void OnInRange(Entity<TailedEntityComponent> ent, ref MeleeInRangeEvent args)
    {
        if (args.Handled || !ent.Comp.MeleeAttackWithSegments)
            return;

        args.Handled = true;

        var segments = ent.Comp.TailSegments.Select(x => GetEntity(x.Segment)).ToList();
        segments.Insert(0, ent);

        foreach (var segment in segments)
        {
            if (!Exists(segment))
                continue;

            if (!CheckInRange(segment, segments, ref args))
                continue;

            args.User = segment;
            return;
        }
    }

    private bool CheckInRange(EntityUid ent, List<EntityUid> segments, ref MeleeInRangeEvent args)
    {
        args.InRange = args.TargetCoordinates is not { } targetCoords || args.TargetAngle is not { } angle
            ? _interaction.InRangeUnobstructed(ent, args.Target, args.Range, predicate: segments.Contains)
            : _interaction.InRangeUnobstructed(ent,
                args.Target,
                targetCoords,
                angle,
                args.Range,
                predicate: segments.Contains,
                overlapCheck: false);
        return args.InRange;
    }

    private void OnGetRange(Entity<TailedEntityComponent> ent, ref GetLightAttackRangeEvent args)
    {
        if (!ent.Comp.MeleeAttackWithSegments || !TryComp(ent, out MeleeWeaponComponent? melee))
            return;

        args.Cancel = true;
        args.Range = ent.Comp.TailSegments.Count + melee.Range;
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_timing.ApplyingState)
            return;

        var query = EntityQueryEnumerator<TailedEntitySegmentComponent>();
        while (query.MoveNext(out var uid, out var segment))
        {
            ResetSegmentPosition((uid, segment));
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.ApplyingState)
            return;

        var query = EntityQueryEnumerator<TailedEntityComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (comp.TailSegments.Count == 0)
                continue;

            UpdateTailPositions((uid, comp, xform));
            UpdateCollision((uid, comp, xform));
        }
    }

    private void UpdateCollision(Entity<TailedEntityComponent, TransformComponent> ent)
    {
        if (!ent.Comp1.ShouldCollideWithSegments || !ent.Comp1.PreventSegmentCollide)
            return;

        _lookSegments.Clear();
        _look.GetEntitiesInRange(ent.Comp2.Coordinates, ent.Comp1.HeadRadius, _lookSegments, LookupFlags.Dynamic);
        if (ent.Comp1.TailSegments.Any(x =>
                TryGetEntity(x.Segment, out var e) && _lookSegments.Any(y => e == y.Owner)))
            return;

        ent.Comp1.PreventSegmentCollide = false;
        Dirty(ent, ent.Comp1);
    }

    protected void UpdateTailPositions(Entity<TailedEntityComponent, TransformComponent> ent)
    {
        if (_timing.ApplyingState)
            return;

        var (uid, comp, xform) = ent;

        var headPos = TransformSystem.GetWorldPosition(xform);

        if (headPos == ent.Comp1.LastPos)
            return;

        ent.Comp1.LastPos = headPos;

        Angle? headRot = null;
        for (var i = 0; i < comp.TailSegments.Count; i++)
        {
            var data = comp.TailSegments[i];

            var segPos = data.WorldPosition;
            var nextPos = i <= 0 ? headPos : comp.TailSegments[i - 1].WorldPosition;
            var nextRot = Angle.FromWorldVec(nextPos - segPos);
            headRot ??= nextRot;

            // Compute the desired position: keep `Spacing` units behind the next entity along the line
            // from the segment to the next entity. If the segment is exactly on top of the target, fall back
            // to using the target's forward vector.
            var toTarget = nextPos - segPos;
            var distance = toTarget.Length();

            Vector2 desiredPos;
            if (distance > 0.0001f)
            {
                var dir = toTarget / distance;
                desiredPos = nextPos - dir * comp.Spacing;
            }
            else
            {
                desiredPos = nextPos - nextRot.ToWorldVec() * comp.Spacing;
            }

            comp.TailSegments[i].WorldPosition = desiredPos;

            if (!TryGetEntity(data.Segment, out var segment) ||
                !SegmentQuery.TryComp(segment.Value, out var segmentComp))
                continue;

            segmentComp.Coords = new MapCoordinates(desiredPos, xform.MapID);
            segmentComp.WorldRotation = nextRot;

            Dirty(segment.Value, segmentComp);

            ResetSegmentPosition((segment.Value, segmentComp));
        }

        if (comp.HeadFollowSegmentRotation && headRot is { } rot)
            TransformSystem.SetWorldRotation(uid, rot);

        Dirty(uid, comp);
    }

    protected void ResetSegmentPosition(Entity<TailedEntitySegmentComponent> segment)
    {
        if (segment.Comp.Coords is { } coords)
            TransformSystem.SetMapCoordinates(segment, coords);
        TransformSystem.SetWorldRotation(segment, segment.Comp.WorldRotation);
    }
}
