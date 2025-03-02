using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared._NF.CrateStorage;
using Content.Shared.Power;

namespace Content.Server._NF.CrateStorage;

public sealed partial class CrateStorageSystem: SharedCrateStorageMachineSystem
{
    /// <summary>
    /// Find the nearest unoccupied anchored crate machine on the same grid.
    /// </summary>
    /// <param name="from">The Uid of the entity to find the nearest crate storage rack from</param>
    /// <param name="maxDistance">The maximum distance to search for a crate storage rack</param>
    /// <param name="isInserting">Whether the entity is inserting or removing a crate</param>
    /// <param name="machineUid">The Uid of the nearest crate storage rack that has space, or null if none found</param>
    /// <returns>True if a crate storage rack was found, false if not</returns>
    private bool FindCrateStorageRack(EntityUid from, float maxDistance, bool isInserting, [NotNullWhen(true)] out EntityUid? machineUid)
    {
        machineUid = null;

        if (maxDistance < 0)
            return false;

        var fromXform = Transform(from);
        if (fromXform.GridUid == null)
            return false;

        var crateMachineQuery = AllEntityQuery<CrateStorageRackComponent, TransformComponent>();
        KeyValuePair<EntityUid, float>? targetRack = null;

        while (crateMachineQuery.MoveNext(out var crateMachineUid, out var comp, out var compXform))
        {
            if (!comp.Powered)
                continue;
            // Skip crate storage racks that aren't mounted on a grid.
            if (compXform.GridUid == null)
                continue;
            // Skip crate storage racks that are not on the same grid.
            if (compXform.GridUid != fromXform.GridUid)
                continue;
            if (isInserting && IsRackFull(crateMachineUid))
                continue;
            if (!isInserting && IsRackEmpty(crateMachineUid))
                continue;

            // We only need local coordinates for distance calculation since we already check for grid equality.
            var distance = Vector2.Distance(compXform.Coordinates.Position, fromXform.Coordinates.Position);

            if (!compXform.Anchored || distance > maxDistance)
            {
                continue;
            }

            // If we are inserting, we want the closest rack. If we are removing, we want the furthest rack.
            if (targetRack != null && (isInserting ? !(distance < targetRack.Value.Value) : !(distance > targetRack.Value.Value)))
                continue;

            machineUid = crateMachineUid;
            targetRack = new KeyValuePair<EntityUid, float>(crateMachineUid, distance);
        }

        if (targetRack == null)
            return false;
        machineUid = targetRack.Value.Key;
        return true;
    }

    private bool IsRackFull(EntityUid rackUid)
    {
        if (!TryComp(rackUid, out CrateStorageRackComponent? rackComponent))
            return false;
        return rackComponent.StoredCrates == rackComponent.Capacity;
    }

    private bool IsRackEmpty(EntityUid rackUid)
    {
        if (!TryComp(rackUid, out CrateStorageRackComponent? rackComponent))
            return false;
        return rackComponent.StoredCrates == 0;
    }

    private void OnRackPowerChanged(EntityUid uid, CrateStorageRackComponent component, PowerChangedEvent args)
    {
        component.Powered = args.Powered;
        Dirty(uid, component);
    }

    private void UpdateRackVisualState(EntityUid uid, CrateStorageRackComponent component)
    {
        if (!TryComp(uid, out AppearanceComponent? appearance))
            return;

        _appearance.SetData(uid, CrateStorageRackVisuals.VisualState, component.StoredCrates);
    }

}

internal class Pair<T, T1>
{
}
