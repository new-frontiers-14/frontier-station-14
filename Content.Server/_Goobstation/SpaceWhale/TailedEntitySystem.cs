// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Movement.Components;
using Content.Shared._Goobstation.SpaceWhale;
using Robust.Shared.Map;

namespace Content.Server._Goobstation.SpaceWhale;

public sealed class TailedEntitySystem : SharedTailedEntitySystem
{
    private EntityQuery<TailedEntitySegmentComponent> _segmentQuery;

    public override void Initialize()
    {
        base.Initialize();
        _segmentQuery = GetEntityQuery<TailedEntitySegmentComponent>();

        SubscribeLocalEvent<TailedEntityComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<TailedEntityComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<TailedEntityComponent, UpdateTailedEntitySegmentCountEvent>(OnUpdate);

        SubscribeLocalEvent<TailedEntitySegmentComponent, ComponentShutdown>(OnSegmentShutdown);
    }

    private void OnUpdate(Entity<TailedEntityComponent> ent, ref UpdateTailedEntitySegmentCountEvent args)
    {
        var difference = args.Amount - ent.Comp.TailSegments.Count;
        switch (difference)
        {
            case > 0:
                AddSegments(ent, difference);
                break;
            case < 0:
                RemoveSegments(ent, -difference);
                break;
        }
    }

    private void OnSegmentShutdown(Entity<TailedEntitySegmentComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent.Comp.Head) || !TryComp(ent.Comp.Head, out TailedEntityComponent? comp))
            return;

        var head = ent.Comp.Head.Value;
        var netEnt = GetNetEntity(ent);

        var index = comp.TailSegments.FindIndex(x => x.Segment == netEnt);
        if (index < 0)
            return;

        for (var i = index + 1; i < comp.TailSegments.Count; i++)
        {
            var data = comp.TailSegments[i];
            var segment = GetEntity(data.Segment);
            if (!_segmentQuery.TryComp(segment, out var otherComp))
                continue;

            otherComp.Order = i - 1;
            otherComp.SegmentCount = comp.TailSegments.Count - 1;
        }

        comp.TailSegments.RemoveAt(index);
        Dirty(head, comp);

        UpdateNoRotateOnMove((head, comp));
        UpdateTailPositions((head, comp, Transform(head)));
    }

    private void OnMapInit(Entity<TailedEntityComponent> ent, ref MapInitEvent args)
    {
        var ev = new GetTailedEntitySegmentCountEvent(ent.Comp.Amount);
        RaiseLocalEvent(ent, ref ev);
        var count = ev.Amount;

        ent.Comp.PreventSegmentCollide = true;
        AddSegments(ent, count);
        UpdateNoRotateOnMove(ent);
    }

    private void OnComponentShutdown(Entity<TailedEntityComponent> ent, ref ComponentShutdown args)
    {
        foreach (var data in ent.Comp.TailSegments)
        {
            if (TryGetEntity(data.Segment, out var segment) && !TerminatingOrDeleted(segment))
                QueueDel(segment);
        }
    }

    private void RemoveSegments(Entity<TailedEntityComponent> ent, int count)
    {
        if (count < 1)
            return;

        var min = Math.Min(count, ent.Comp.TailSegments.Count);
        for (var i = 0; i < min; i++)
        {
            var segment = ent.Comp.TailSegments[^(i + 1)];
            QueueDel(GetEntity(segment.Segment));
        }
    }

    private void UpdateNoRotateOnMove(Entity<TailedEntityComponent> ent)
    {
        if (!ent.Comp.HeadFollowSegmentRotation)
            return;

        if (ent.Comp.TailSegments.Count == 0)
            RemCompDeferred<NoRotateOnMoveComponent>(ent);
        else
            EnsureComp<NoRotateOnMoveComponent>(ent);
    }

    private void AddSegments(Entity<TailedEntityComponent> ent, int count)
    {
        if (count < 1)
            return;

        var xform = Transform(ent);

        var (headPos, headRot) = TransformSystem.GetWorldPositionRotation(xform);
        var coords = new MapCoordinates(headPos, xform.MapID);

        for (var i = 0; i < count; i++)
        {
            var segment = Spawn(ent.Comp.Prototype, coords);
            var segmentComp = EnsureComp<TailedEntitySegmentComponent>(segment);
            segmentComp.Coords = coords;
            segmentComp.WorldRotation = headRot;
            segmentComp.Order = i + ent.Comp.Amount;
            segmentComp.SegmentCount = count;
            segmentComp.Head = ent;
            Dirty(segment, segmentComp);
            ent.Comp.TailSegments.Add(new (GetNetEntity(segment), headPos));
        }

        Dirty(ent);
    }
}
