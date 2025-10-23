using System.Linq;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.NameIdentifier;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Collections;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Content.Shared.Implants; // Frontier
using Content.Shared.Implants.Components; // Frontier
using Content.Shared.Radio.Components; // Frontier
using Robust.Shared.Containers; // Frontier
using Robust.Shared.Network; // Frontier

namespace Content.Shared.Station;

public abstract class SharedStationSpawningSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] protected readonly InventorySystem InventorySystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly SharedTransformSystem _xformSystem = default!;
    [Dependency] private readonly INetManager _net = default!; // Frontier
    [Dependency] private readonly SharedContainerSystem _container = default!; // Frontier
    [Dependency] private readonly SharedImplanterSystem _implanter = default!; // Frontier

    private EntityQuery<HandsComponent> _handsQuery;
    private EntityQuery<InventoryComponent> _inventoryQuery;
    private EntityQuery<StorageComponent> _storageQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    public override void Initialize()
    {
        base.Initialize();
        _handsQuery = GetEntityQuery<HandsComponent>();
        _inventoryQuery = GetEntityQuery<InventoryComponent>();
        _storageQuery = GetEntityQuery<StorageComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();
    }

    /// <summary>
    ///     Equips the data from a `RoleLoadout` onto an entity.
    /// </summary>
    /// <remarks>
    ///     Frontier: must run on the server, requires bank access.
    ///     Frontier: currently not charging the player for this.
    /// </remarks>
    public void EquipRoleLoadout(EntityUid entity, RoleLoadout loadout, RoleLoadoutPrototype roleProto)
    {
        // Order loadout selections by the order they appear on the prototype.
        foreach (var group in loadout.SelectedLoadouts.OrderBy(x => roleProto.Groups.FindIndex(e => e == x.Key)))
        {
            List<ProtoId<LoadoutPrototype>> equippedItems = new(); //Frontier - track purchased items (list: few items)
            foreach (var items in group.Value)
            {
                if (!PrototypeManager.TryIndex(items.Prototype, out var loadoutProto))
                {
                    Log.Error($"Unable to find loadout prototype for {items.Prototype}");
                    continue;
                }

                EquipStartingGear(entity, loadoutProto, raiseEvent: false);
                equippedItems.Add(loadoutProto.ID); // Frontier
            }

            // If a character cannot afford their current job loadout, ensure they have fallback items for mandatory categories.
            if (PrototypeManager.TryIndex(group.Key, out var groupPrototype) &&
                equippedItems.Count < groupPrototype.MinLimit)
            {
                foreach (var fallback in groupPrototype.Fallbacks)
                {
                    // Do not duplicate items in loadout
                    if (equippedItems.Contains(fallback))
                    {
                        continue;
                    }

                    if (!PrototypeManager.TryIndex(fallback, out var loadoutProto))
                    {
                        Log.Error($"Unable to find loadout prototype for fallback {fallback}");
                        continue;
                    }

                    EquipStartingGear(entity, loadoutProto, raiseEvent: false);
                    equippedItems.Add(fallback);
                    // Minimum number of items equipped, no need to load more prototypes.
                    if (equippedItems.Count >= groupPrototype.MinLimit)
                        break;
                }
            }
            // End Frontier
        }

        EquipRoleName(entity, loadout, roleProto);
    }

    /// <summary>
    /// Applies the role's name as applicable to the entity.
    /// </summary>
    public void EquipRoleName(EntityUid entity, RoleLoadout loadout, RoleLoadoutPrototype roleProto)
    {
        string? name = null;

        if (roleProto.CanCustomizeName)
        {
            name = loadout.EntityName;
        }

        if (string.IsNullOrEmpty(name) && PrototypeManager.TryIndex(roleProto.NameDataset, out var nameData))
        {
            name = Loc.GetString(_random.Pick(nameData.Values));
        }

        if (!string.IsNullOrEmpty(name))
        {
            _metadata.SetEntityName(entity, name);
        }
    }

    public void EquipStartingGear(EntityUid entity, LoadoutPrototype loadout, bool raiseEvent = true)
    {
        EquipStartingGear(entity, loadout.StartingGear, raiseEvent);
        EquipStartingGear(entity, (IEquipmentLoadout)loadout, raiseEvent);
    }

    /// <summary>
    /// <see cref="EquipStartingGear(Robust.Shared.GameObjects.EntityUid,System.Nullable{Robust.Shared.Prototypes.ProtoId{Content.Shared.Roles.StartingGearPrototype}},bool)"/>
    /// </summary>
    public void EquipStartingGear(EntityUid entity, ProtoId<StartingGearPrototype>? startingGear, bool raiseEvent = true)
    {
        PrototypeManager.TryIndex(startingGear, out var gearProto);
        EquipStartingGear(entity, gearProto, raiseEvent);
    }

    /// <summary>
    /// <see cref="EquipStartingGear(Robust.Shared.GameObjects.EntityUid,System.Nullable{Robust.Shared.Prototypes.ProtoId{Content.Shared.Roles.StartingGearPrototype}},bool)"/>
    /// </summary>
    public void EquipStartingGear(EntityUid entity, StartingGearPrototype? startingGear, bool raiseEvent = true)
    {
        EquipStartingGear(entity, (IEquipmentLoadout?)startingGear, raiseEvent);
    }

    /// <summary>
    /// Equips starting gear onto the given entity.
    /// </summary>
    /// <param name="entity">Entity to load out.</param>
    /// <param name="startingGear">Starting gear to use.</param>
    /// <param name="raiseEvent">Should we raise the event for equipped. Set to false if you will call this manually</param>
    public void EquipStartingGear(EntityUid entity, IEquipmentLoadout? startingGear, bool raiseEvent = true)
    {
        if (startingGear == null)
            return;

        var xform = _xformQuery.GetComponent(entity);

        if (InventorySystem.TryGetSlots(entity, out var slotDefinitions))
        {
            foreach (var slot in slotDefinitions)
            {
                var equipmentStr = startingGear.GetGear(slot.Name);
                if (!string.IsNullOrEmpty(equipmentStr))
                {
                    var equipmentEntity = EntityManager.SpawnEntity(equipmentStr, xform.Coordinates);
                    InventorySystem.TryEquip(entity, equipmentEntity, slot.Name, silent: true, force: true);
                }
            }
        }

        if (_handsQuery.TryComp(entity, out var handsComponent))
        {
            var inhand = startingGear.Inhand;
            var coords = xform.Coordinates;
            foreach (var prototype in inhand)
            {
                var inhandEntity = EntityManager.SpawnEntity(prototype, coords);

                if (_handsSystem.TryGetEmptyHand(entity, out var emptyHand, handsComponent))
                {
                    _handsSystem.TryPickup(entity, inhandEntity, emptyHand, checkActionBlocker: false, handsComp: handsComponent);
                }
            }
        }

        if (startingGear.Storage.Count > 0)
        {
            var coords = _xformSystem.GetMapCoordinates(entity);
            _inventoryQuery.TryComp(entity, out var inventoryComp);

            foreach (var (slotName, entProtos) in startingGear.Storage)
            {
                if (entProtos == null || entProtos.Count == 0)
                    continue;

                if (inventoryComp != null &&
                    InventorySystem.TryGetSlotEntity(entity, slotName, out var slotEnt, inventoryComponent: inventoryComp) &&
                    _storageQuery.TryComp(slotEnt, out var storage))
                {

                    foreach (var entProto in entProtos)
                    {
                        var spawnedEntity = Spawn(entProto, coords);

                        _storage.Insert(slotEnt.Value, spawnedEntity, out _, storageComp: storage, playSound: false);
                    }
                }
            }
        }

        // Frontier: extra fields
        // Implants must run on server, container initialization only runs on server, and lobby dummies don't work.
        if (_net.IsServer && startingGear.Implants.Count > 0)
        {
            var coords = _xformSystem.GetMapCoordinates(entity);
            foreach (var entProto in startingGear.Implants)
            {
                var spawnedEntity = Spawn(entProto, coords);
                if (TryComp<ImplanterComponent>(spawnedEntity, out var implanter))
                    _implanter.Implant(entity, entity, spawnedEntity, implanter);
                else
                    DebugTools.Assert(false, $"Entity has an implant for {entProto}, which doesn't have an implanter component!");
                QueueDel(spawnedEntity);
            }
        }

        if (startingGear.EncryptionKeys.Count > 0)
        {
            EquipEncryptionKeysIfPossible(entity, startingGear.EncryptionKeys);
        }

        // PDA cartridges must run on server, installation logic exists server-side.
        if (_net.IsServer && startingGear.Cartridges.Count > 0)
        {
            EquipPdaCartridgesIfPossible(entity, startingGear.Cartridges);
        }
        // End Frontier

        if (raiseEvent)
        {
            var ev = new StartingGearEquippedEvent(entity);
            RaiseLocalEvent(entity, ref ev);
        }
    }

    /// <summary>
    ///     Gets all the gear for a given slot when passed a loadout.
    /// </summary>
    /// <param name="loadout">The loadout to look through.</param>
    /// <param name="slot">The slot that you want the clothing for.</param>
    /// <returns>
    ///     If there is a value for the given slot, it will return the proto id for that slot.
    ///     If nothing was found, will return null
    /// </returns>
    public string? GetGearForSlot(RoleLoadout? loadout, string slot)
    {
        if (loadout == null)
            return null;

        foreach (var group in loadout.SelectedLoadouts)
        {
            foreach (var items in group.Value)
            {
                if (!PrototypeManager.TryIndex(items.Prototype, out var loadoutPrototype))
                    return null;

                var gear = ((IEquipmentLoadout)loadoutPrototype).GetGear(slot);
                if (gear != string.Empty)
                    return gear;
            }
        }

        return null;
    }

    // Frontier: extra loadout fields
    /// Function to equip an entity with encryption keys.
    /// If not possible, will delete them.
    /// Only called in practice server-side.
    /// </summary>
    /// <param name="entity">The entity to receive equipment.</param>
    /// <param name="encryptionKeys">The encryption key prototype IDs to equip.</param>
    protected void EquipEncryptionKeysIfPossible(EntityUid entity, List<EntProtoId> encryptionKeys)
    {
        if (!InventorySystem.TryGetSlotEntity(entity, "ears", out var slotEnt))
        {
            DebugTools.Assert(false, $"Entity {entity} has a non-empty encryption key loadout, but doesn't have a headset!");
            return;
        }
        if (!_container.TryGetContainer(slotEnt.Value, EncryptionKeyHolderComponent.KeyContainerName, out var keyContainer))
        {
            DebugTools.Assert(false, $"Entity {entity} has a non-empty encryption key loadout, but their headset doesn't have an encryption key container!");
            return;
        }
        var coords = _xformSystem.GetMapCoordinates(entity);
        foreach (var entProto in encryptionKeys)
        {
            var spawnedEntity = Spawn(entProto, coords);
            if (!_container.Insert(spawnedEntity, keyContainer))
            {
                QueueDel(spawnedEntity);
                DebugTools.Assert(false, $"Entity {entity} could not insert their loadout encryption key {entProto} into their headset!");
            }
        }
    }

    /// <summary>
    /// Function to equip an entity with PDA cartridges.
    /// If not possible, will delete them.
    /// Only called in practice server-side.
    /// </summary>
    /// <param name="entity">The entity to receive equipment.</param>
    /// <param name="encryptionKeys">The PDA cartridge prototype IDs to equip.</param>
    protected abstract void EquipPdaCartridgesIfPossible(EntityUid entity, List<EntProtoId> encryptionKeys);
    // End Frontier: extra loadout fields
}
