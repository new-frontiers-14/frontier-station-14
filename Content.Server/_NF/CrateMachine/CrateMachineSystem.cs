using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Storage.EntitySystems;
using Content.Shared._NF.CrateMachine;
using Content.Shared._NF.CrateMachine.Components;
using Content.Shared.Maps;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

namespace Content.Server._NF.CrateMachine;

/// <summary>
/// The crate machine system can be used to make a crate machine open and spawn crates.
/// When calling <see cref="OpenFor"/>, the machine will open the door and give a callback to the given
/// </summary>
public sealed partial class CrateMachineSystem : SharedCrateMachineSystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly EntityStorageSystem _storage = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    /// <summary>
    /// Checks if there is a crate on the crate machine.
    /// </summary>
    /// <param name="crateMachineUid">The Uid of the crate machine</param>
    /// <param name="component">The crate machine component</param>
    /// <param name="ignoreAnimation">Ignores animation checks</param>
    /// <returns>False if not occupied, true if it is.</returns>
    public bool IsOccupied(EntityUid crateMachineUid, CrateMachineComponent component, bool ignoreAnimation = false)
    {
        if (!TryComp(crateMachineUid, out TransformComponent? crateMachineTransform))
            return true;
        var tileRef = crateMachineTransform.Coordinates.GetTileRef(EntityManager, _mapManager);
        if (tileRef == null)
            return true;

        if (!ignoreAnimation && (component.OpeningTimeRemaining > 0 || component.ClosingTimeRemaining > 0f))
            return true;

        // Finally check if there is a crate intersecting the crate machine.
        return _lookup.GetLocalEntitiesIntersecting(tileRef.Value, flags: LookupFlags.All | LookupFlags.Approximate)
            .Any(entity => MetaData(entity).EntityPrototype?.ID ==
                           component.CratePrototype);
    }

    /// <summary>
    /// Find the nearest unoccupied anchored crate machine on the same grid.
    /// </summary>
    /// <param name="from">The Uid of the entity to find the nearest crate machine from</param>
    /// <param name="maxDistance">The maximum distance to search for a crate machine</param>
    /// <param name="machineUid">The Uid of the nearest unoccupied crate machine, or null if none found</param>
    /// <returns>True if a crate machine was found, false if not</returns>
    public bool FindNearestUnoccupied(EntityUid from, int maxDistance, [NotNullWhen(true)] out EntityUid? machineUid)
    {
        machineUid = null;

        if (maxDistance < 0)
            return false;

        var fromXform = Transform(from);
        if (fromXform.GridUid == null)
            return false;

        var crateMachineQuery = AllEntityQuery<CrateMachineComponent, TransformComponent>();
        while (crateMachineQuery.MoveNext(out var crateMachineUid, out var comp, out var compXform))
        {
            // Skip crate machines that aren't mounted on a grid.
            if (compXform.GridUid == null)
                continue;
            // Skip crate machines that are not on the same grid.
            if (compXform.GridUid != fromXform.GridUid)
                continue;

            var isTooFarAway = !_transform.InRange(compXform.Coordinates, fromXform.Coordinates, maxDistance);
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
