using System.Numerics;
using Content.Server.StationEvents.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using Robust.Shared.Map;

namespace Content.Server.StationEvents.Events;

public sealed class LinkedLifecycleGridSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LinkedLifecycleGridParentComponent, GridSplitEvent>(OnParentSplit);
        SubscribeLocalEvent<LinkedLifecycleGridChildComponent, GridSplitEvent>(OnChildSplit);

        SubscribeLocalEvent<LinkedLifecycleGridParentComponent, ComponentRemove>(OnMasterRemoved);
    }

    private void OnParentSplit(EntityUid uid, LinkedLifecycleGridParentComponent component, ref GridSplitEvent args)
    {
        LinkSplitGrids(uid, ref args);
    }

    private void OnChildSplit(EntityUid uid, LinkedLifecycleGridChildComponent component, ref GridSplitEvent args)
    {
        LinkSplitGrids(component.LinkedUid, ref args);
    }

    private void LinkSplitGrids(EntityUid target, ref GridSplitEvent args)
    {
        if (!TryComp(target, out LinkedLifecycleGridParentComponent? master))
            return;

        foreach (var grid in args.NewGrids)
        {
            if (grid == target)
                continue;

            var comp = EnsureComp<LinkedLifecycleGridChildComponent>(grid);
            comp.LinkedUid = target;
            master.LinkedEntities.Add(grid);
        }
    }

    private void OnMasterRemoved(EntityUid uid, LinkedLifecycleGridParentComponent component, ref ComponentRemove args)
    {
        // Somebody destroyed our component, but the entity lives on, do not destroy the grids.
        if (MetaData(uid).EntityLifeStage < EntityLifeStage.Terminating)
            return;

        // Destroy child entities
        foreach (var entity in component.LinkedEntities)
            DeleteGrid(entity);
    }

    public void DeleteGrid(EntityUid grid)
    {
        var mobQuery = AllEntityQuery<HumanoidAppearanceComponent, MobStateComponent, TransformComponent>();
        List<(Entity<TransformComponent> Entity, EntityUid MapUid, Vector2 LocalPosition)> playerMobs = new();

        while (mobQuery.MoveNext(out var mobUid, out _, out _, out var xform))
        {
            if (xform.GridUid == null || xform.MapUid == null || xform.GridUid != grid)
                continue;

            // Can't parent directly to map as it runs grid traversal.
            playerMobs.Add(((mobUid, xform), xform.MapUid.Value, _transform.GetWorldPosition(xform)));
            _transform.DetachEntity(mobUid, xform);
        }

        // Deletion has to happen before grid traversal re-parents players.
        Del(grid);

        foreach (var mob in playerMobs)
        {
            _transform.SetCoordinates(mob.Entity.Owner, new EntityCoordinates(mob.MapUid, mob.LocalPosition));
        }
    }
}
