// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Goobstation.SpaceWhale;
using Robust.Client.GameObjects;

namespace Content.Client._Goobstation.SpaceWhale;

public sealed class TailedEntitySystem : SharedTailedEntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly EntityQuery<SpriteComponent> _spriteQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        TransformSystem.OnGlobalMoveEvent += OnMove;

        SubscribeLocalEvent<TailedEntityComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
        SubscribeLocalEvent<TailedEntitySegmentComponent, AfterAutoHandleStateEvent>(OnSegmentAfterAutoHandleState);
    }

    private void OnAfterAutoHandleState(Entity<TailedEntityComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (_spriteQuery.TryGetComponent(ent.Owner, out var sprite))
            sprite.RenderOrder = (uint) ent.Comp.TailSegments.Count + 5;
    }

    private void OnSegmentAfterAutoHandleState(Entity<TailedEntitySegmentComponent> ent,
        ref AfterAutoHandleStateEvent args)
    {
        if (!_spriteQuery.TryGetComponent(ent.Owner, out var sprite))
            return;

        sprite.RenderOrder = (uint) (ent.Comp.SegmentCount - ent.Comp.Order + 5);

        if (ent.Comp.SegmentSpriteState is not { } segmentState || ent.Comp.TailSpriteState is not { } tailState)
            return;

        _sprite.LayerSetRsiState((ent, sprite),
            TailedEntitySegmentLayer.Base,
            ent.Comp.Order == ent.Comp.SegmentCount - 1 ? tailState : segmentState);
    }

    private void OnMove(ref MoveEvent args)
    {
        if (args.OldPosition == args.NewPosition && args.OldRotation == args.NewRotation ||
            TerminatingOrDeleted(args.Entity) ||
            !TryComp(args.Entity, out TailedEntityComponent? tailed) || tailed.TailSegments.Count == 0)
            return;

        UpdateTailPositions((args.Entity, tailed, args.Entity.Comp1));
    }
}
