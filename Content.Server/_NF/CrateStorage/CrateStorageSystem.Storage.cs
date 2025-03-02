using System.Linq;
using System.Numerics;
using Content.Server._NF.CrateMachine;
using Content.Server.DeviceLinking.Events;
using Content.Shared._NF.CrateMachine.Components;
using Content.Shared._NF.CrateStorage;
using Content.Shared._NF.Trade;
using Content.Shared.Placeable;
using Robust.Shared.Map;

namespace Content.Server._NF.CrateStorage;

public sealed partial class CrateStorageSystem: SharedCrateStorageMachineSystem
{
    // A Record struct will benefit performance for the dictionary.
    private record struct StoredCrate
    {
        public EntityUid CrateUid; // The crate.
        public EntityUid CrateStorageRack; // Which rack it is stored in.
    }

    // Dictionary where the key is the entity id of the crate storage and the value is the list of stored crates.
    private readonly Dictionary<EntityUid, List<StoredCrate>> _storedCrates = new();
    private EntityUid? _storageMap;

    /// <summary>
    /// Once a crate machine is opened we will check if there are any crates intersecting it.
    /// </summary>
    /// <param name="crateMachineUid">the EntityUid of the crate </param>
    /// <param name="crateStorageMachineComponent">the crate storage component</param>
    /// <param name="args">the arguments for the open event</param>
    private void OnCrateMachineOpened(EntityUid crateMachineUid, CrateStorageMachineComponent crateStorageMachineComponent, CrateMachineOpenedEvent args)
    {
        CheckIntersectingCrates(crateMachineUid, crateStorageMachineComponent.PickupRange, crateStorageMachineComponent.StorageRackSearchRange);
    }

    /// <summary>
    /// Processes a signal received event to open a crate storage.
    /// </summary>
    /// <param name="crateStorageUid">the EntityUid for the crate storage</param>
    /// <param name="crateStorageMachineComponent">the crate storage component</param>
    /// <param name="args">the args for the signal</param>
    private void OnSignalReceived(EntityUid crateStorageUid, CrateStorageMachineComponent crateStorageMachineComponent, SignalReceivedEvent args)
    {
        // Get the CrateMachineComponent from the entity.
        if (!TryComp(crateStorageUid, out CrateMachineComponent? crateMachineComponent))
            return;

        // Can't receive signals if we're not powered.
        if (!crateMachineComponent.Powered)
            return;

        if (args.Port == crateStorageMachineComponent.TriggerPort)
            _crateMachineSystem.StartOpening(crateStorageUid, crateMachineComponent);
    }

    private void OnItemPlacedEvent(EntityUid crateStorageUid, CrateStorageMachineComponent component, ItemPlacedEvent args)
    {
        // TODO
    }

    private EntityUid GetStorageMap()
    {
        if (!Deleted(_storageMap))
            return _storageMap.Value;

        _storageMap = _mapSystem.CreateMap(out var mapId);
        _mapManager.SetMapPaused(mapId, true);
        return _storageMap.Value;
    }

    /// <summary>
    /// Stores a crate in a crate storage map.
    /// </summary>
    /// <param name="crateStorageUid">the EntityUid of the crate storage</param>
    /// <param name="crateUid">the EntityUid of the crate that is to be stored</param>
    /// <param name="storageSearchRange">The range to look for storage racks</param>
    private void StoreCrate(EntityUid crateStorageUid, EntityUid crateUid, float storageSearchRange)
    {
        if (!_storedCrates.TryGetValue(crateStorageUid, out var storedCrates))
        {
            storedCrates = [];
            _storedCrates.Add(crateStorageUid, storedCrates);
        }

        // Attempt to find a storage rack to store the crate in.
        if (!FindCrateStorageRack(crateStorageUid, storageSearchRange, true, out var rackUid))
            return;
        if (!TryComp<CrateStorageRackComponent>(rackUid.Value, out var rackComp))
            return;
        rackComp.StoredCrates++;
        UpdateRackVisualState(rackUid.Value, rackComp);
        Dirty(rackUid.Value, rackComp);

        storedCrates.Add(new StoredCrate { CrateUid = crateUid, CrateStorageRack = rackUid.Value });
        _transformSystem.SetCoordinates(crateUid, new EntityCoordinates(GetStorageMap(), Vector2.Zero));
    }

    /// <summary>
    /// Ejects the last crate stored in the crate storage. (First in first out)
    /// </summary>
    /// <param name="crateStorageUid">the EntityUid of the crate storage</param>
    /// /// <param name="storageSearchRange">The range to look for storage racks</param>
    private void EjectCrate(EntityUid crateStorageUid, float storageSearchRange)
    {
        // Get the crate from the storage.
        if (!_storedCrates.TryGetValue(crateStorageUid, out var storedCrates))
            return;

        if (storedCrates.Count == 0)
            return;

        // Attempt to find a storage rack to get a crate from.
        if (!FindCrateStorageRack(crateStorageUid, storageSearchRange, false, out var rackUid))
            return;
        if (!TryComp<CrateStorageRackComponent>(rackUid.Value, out var rackComp))
            return;
        rackComp.StoredCrates--;
        UpdateRackVisualState(rackUid.Value, rackComp);
        Dirty(rackUid.Value, rackComp);

        // Remove it from storedCrates
        var crate = storedCrates.First();
        storedCrates.Remove(crate);

        // Transport it to the crate storage machine.
        _transformSystem.SetCoordinates(crate.CrateUid, Transform(crateStorageUid).Coordinates);
    }

    /// <summary>
    /// Two functions: if there is a crate, move it. If there is none, eject a stored crate.
    /// </summary>
    /// <param name="crateStorageUid">the EntityUid of the crate storage</param>
    /// <param name="pickupRange">The range to look for crates</param>
    /// <param name="storageSearchRange">The range to look for storage racks</param>
    private void CheckIntersectingCrates(EntityUid crateStorageUid, float pickupRange, float storageSearchRange)
    {
        foreach (var near in _lookup.GetEntitiesInRange(crateStorageUid, pickupRange, LookupFlags.Dynamic)
                     .Where(near => TryComp(near, out TradeCrateComponent? _)))
        {
            StoreCrate(crateStorageUid, near, storageSearchRange);
            return; // We found a crate and moved it, so we can stop here.
        }

        // At this point we haven't found any crates, so we should eject one.
        EjectCrate(crateStorageUid, storageSearchRange);
    }
}
