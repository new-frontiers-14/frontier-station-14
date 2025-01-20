using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Storage.EntitySystems;
using Content.Shared._NF.CrateMachine.Components;
using Content.Shared.Maps;
using Robust.Shared.Map;

namespace Content.Server._NF.CrateMachine;

/// <summary>
/// The crate machine system can be used to make a crate machine open and spawn crates.
/// When calling <see cref="OpenFor"/>, the machine will open the door and give a callback to the given
/// <see cref="ICrateMachineDelegate"/> when it is done opening.
/// </summary>
public sealed partial class CrateMachineSystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly EntityStorageSystem _storage = default!;

    /// <summary>
    /// Checks if there is a crate on the crate machine.
    /// </summary>
    /// <param name="crateMachineUid">The Uid of the crate machine</param>
    /// <param name="component">The crate machine component</param>
    /// <param name="ignoreAnimation">Ignores animation checks</param>
    /// <returns>False if not occupied, true if it is.</returns>
    public bool IsOccupied(EntityUid crateMachineUid, CrateMachineComponent component, bool ignoreAnimation = false)
    {
        if (!_entityManager.TryGetComponent<TransformComponent>(crateMachineUid, out var crateMachineTransform))
            return true;
        var tileRef = crateMachineTransform.Coordinates.GetTileRef(EntityManager, _mapManager);
        if (tileRef == null)
            return true;

        if (!ignoreAnimation && (component.OpeningTimeRemaining > 0 || component.ClosingTimeRemaining > 0f))
            return true;

        // Finally check if there is a crate intersecting the crate machine.
        return _lookup.GetLocalEntitiesIntersecting(tileRef.Value, flags: LookupFlags.All | LookupFlags.Approximate)
            .Any(entity => _entityManager.GetComponent<MetaDataComponent>(entity).EntityPrototype?.ID ==
                           component.CratePrototype);
    }

    /// <summary>
    /// Calculates distance between two EntityCoordinates on the same grid.
    /// Used to check for cargo pallets around the console instead of on the grid.
    /// </summary>
    /// <param name="point1">the first point</param>
    /// <param name="point2">the second point</param>
    /// <returns></returns>
    private static double CalculateDistance(EntityCoordinates point1, EntityCoordinates point2)
    {
        var xDifference = point2.X - point1.X;
        var yDifference = point2.Y - point1.Y;

        return Math.Sqrt(xDifference * xDifference + yDifference * yDifference);
    }

    /// <summary>
    /// Find the nearest unoccupied crate machine, that is anchored.
    /// </summary>
    /// <param name="from">The Uid of the entity to find the nearest crate machine from</param>
    /// <param name="maxDistance">The maximum distance to search for a crate machine</param>
    /// <param name="machineUid">The Uid of the nearest unoccupied crate machine, or null if none found</param>
    public bool FindNearestUnoccupied(EntityUid from, int maxDistance, [NotNullWhen(true)] out EntityUid? machineUid)
    {
        machineUid = null;
        if (maxDistance < 0)
            return false;

        var crateMachineQuery = AllEntityQuery<CrateMachineComponent, TransformComponent>();
        var consoleGridUid = Transform(from).GridUid!.Value;
        while (crateMachineQuery.MoveNext(out var crateMachineUid, out var comp, out var compXform))
        {
            // Skip crate machines that aren't mounted on a grid.
            if (Transform(crateMachineUid).GridUid == null)
                continue;
            // Skip crate machines that are not on the same grid.
            if (Transform(crateMachineUid).GridUid!.Value != consoleGridUid)
                continue;
            var currentDistance = CalculateDistance(compXform.Coordinates, Transform(from).Coordinates);

            var isTooFarAway = currentDistance > maxDistance;
            var isBusy = IsOccupied(crateMachineUid, comp);

            if (!compXform.Anchored || isTooFarAway || isBusy)
            {
                continue;
            }

            machineUid = crateMachineUid;
            return true;
        }
        return false;
    }


    /// <summary>
    /// Convenience function that simply spawns a crate and returns the uid.
    /// </summary>
    /// <param name="uid">The Uid of the crate machine</param>
    /// <param name="component">The crate machine component</param>
    /// <returns>The Uid of the spawned crate</returns>
    public EntityUid SpawnCrate(EntityUid uid, CrateMachineComponent component)
    {
        return Spawn(component.CratePrototype, Transform(uid).Coordinates);
    }

    /// <summary>
    /// Convenience function that simply inserts a entity into the container entity and relieve the caller of
    /// using the storage system reference.
    /// </summary>
    /// <param name="uid">The Uid of the crate machine</param>
    /// <param name="container">The Uid of the container</param>
    public void InsertIntoCrate(EntityUid uid, EntityUid container)
    {
        _storage.Insert(uid, container);
    }
}
