using System.Numerics;
using Content.Server.StationEvents.Components;
using Content.Shared.Buckle.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mech.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Vehicle.Components;
using Robust.Shared.Map;

namespace Content.Server.StationEvents.Events;

public sealed class LinkedLifecycleGridSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

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
            UnparentPlayersFromGrid(entity, true);
    }

    // Try to get parent of entity where appropriate.
    private (EntityUid, TransformComponent) GetParentToReparent(EntityUid uid, TransformComponent xform)
    {
        if (TryComp<RiderComponent>(uid, out var rider) && rider.Vehicle != null)
        {
            var vehicleXform = Transform(rider.Vehicle.Value);
            if (vehicleXform.MapUid != null)
            {
                return (rider.Vehicle.Value, vehicleXform);
            }
        }
        if (TryComp<MechPilotComponent>(uid, out var mechPilot))
        {
            var mechXform = Transform(mechPilot.Mech);
            if (mechXform.MapUid != null)
            {
                return (mechPilot.Mech, mechXform);
            }
        }
        return (uid, xform);
    }

    // Returns a list of entities to reparent on a grid.
    // Useful if you need to do your own bookkeeping.
    public List<(Entity<TransformComponent> Entity, EntityUid MapUid, Vector2 LocalPosition)> GetEntitiesToReparent(EntityUid grid)
    {
        List<(Entity<TransformComponent> Entity, EntityUid MapUid, Vector2 LocalPosition)> reparentEntities = new();
        HashSet<EntityUid> handledEntities = new();

        // Get humanoids
        var mobQuery = AllEntityQuery<HumanoidAppearanceComponent, MobStateComponent, TransformComponent>();
        while (mobQuery.MoveNext(out var mobUid, out _, out _, out var xform))
        {
            handledEntities.Add(mobUid);

            if (xform.GridUid == null || xform.MapUid == null || xform.GridUid != grid)
                continue;

            var (targetUid, targetXform) = GetParentToReparent(mobUid, xform);

            reparentEntities.Add(((targetUid, targetXform), targetXform.MapUid!.Value, _transform.GetWorldPosition(targetXform)));
        }

        // Get occupied MindContainers
        var mindQuery = AllEntityQuery<MindContainerComponent, TransformComponent>();
        while (mindQuery.MoveNext(out var mobUid, out var mindContainer, out var xform))
        {
            if (xform.GridUid == null || xform.MapUid == null || xform.GridUid != grid)
                continue;

            // Not player-controlled, nothing to lose
            if (_mind.GetMind(mobUid, mindContainer) == null)
                continue;

            // Already handled
            if (handledEntities.Contains(mobUid))
                continue;

            var (targetUid, targetXform) = GetParentToReparent(mobUid, xform);

            reparentEntities.Add(((targetUid, targetXform), targetXform.MapUid!.Value, _transform.GetWorldPosition(targetXform)));
        }

        return reparentEntities;
    }

    // Deletes a grid, reparenting every humanoid and player character that's on it.
    public void UnparentPlayersFromGrid(EntityUid grid, bool deleteGrid)
    {
        if (MetaData(grid).EntityLifeStage >= EntityLifeStage.Terminating)
            return;

        var reparentEntities = GetEntitiesToReparent(grid);

        foreach (var target in reparentEntities)
        {
            // Move the target and all of its children (for bikes, mechs, etc.)
            _transform.DetachEntity(target.Entity.Owner, target.Entity.Comp);
        }

        // Deletion has to happen before grid traversal re-parents players.
        if (deleteGrid)
            Del(grid);

        foreach (var target in reparentEntities)
        {
            _transform.SetCoordinates(target.Entity.Owner, new EntityCoordinates(target.MapUid, target.LocalPosition));
        }
    }
}
