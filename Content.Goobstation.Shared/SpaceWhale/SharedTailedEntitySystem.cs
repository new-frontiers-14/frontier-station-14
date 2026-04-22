// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Goobstation.Common.Weapons;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Physics.Events;
using Robust.Shared.Map;

namespace Content.Goobstation.Shared.SpaceWhale;

public abstract class SharedTailedEntitySystem : EntitySystem
{
    [Dependency] protected readonly SharedTransformSystem TransformSystem = default!;

    [Dependency] private readonly SharedInteractionSystem _interaction = default!;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesOutsidePrediction = true;

        SubscribeLocalEvent<TailedEntityComponent, GetLightAttackRangeEvent>(OnGetRange);
        SubscribeLocalEvent<TailedEntityComponent, MeleeInRangeEvent>(OnInRange);
        SubscribeLocalEvent<TailedEntityComponent, AttackAttemptEvent>(OnAttackAttempt);
    }

    private void OnAttackAttempt(Entity<TailedEntityComponent> ent, ref AttackAttemptEvent args)
    {
        if (args.Target is { } target && ent.Comp.TailSegments.Contains(target))
            args.Cancel();
    }

    private void OnInRange(Entity<TailedEntityComponent> ent, ref MeleeInRangeEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var segments = new List<EntityUid> { ent };
        segments.AddRange(ent.Comp.TailSegments);

        foreach (var segment in segments)
        {
            if (!Exists(segment))
                return;

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
        if (!TryComp(ent, out MeleeWeaponComponent? melee))
            return;

        args.Cancel = true;
        args.Range = ent.Comp.TailSegments.Count + melee.Range;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TailedEntityComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            UpdateTailPositions((uid, comp, xform));
            UpdateTailLayers((uid, comp));
        }
    }

    protected virtual void UpdateTailLayers(Entity<TailedEntityComponent> ent) { }

    private void UpdateTailPositions(Entity<TailedEntityComponent, TransformComponent> ent)
    {
        var (uid, comp, xform) = ent;

        // Use the head's world position to determine whether anything moved.
        var headPos = TransformSystem.GetWorldPosition(xform);
        if (headPos == comp.LastPos)
            return;

        Angle? headRot = null;
        for (var i = 0; i < comp.TailSegments.Count; i++)
        {
            var segment = comp.TailSegments[i];

            if (TerminatingOrDeleted(segment))
                continue;

            EntityUid? next = i <= 0 ? uid : comp.TailSegments[i - 1];

            if (TerminatingOrDeleted(next))
                continue;

            var segPos = TransformSystem.GetWorldPosition(segment);
            var nextPos = TransformSystem.GetWorldPosition(next.Value);
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

            TransformSystem.SetMapCoordinates(segment, new MapCoordinates(desiredPos, xform.MapID));
            TransformSystem.SetWorldRotation(segment, nextRot);
        }

        if (headRot is { } rot)
            TransformSystem.SetWorldRotation(ent.Owner, rot);
        comp.LastPos = headPos;
    }
}
