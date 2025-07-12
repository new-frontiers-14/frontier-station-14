using Content.Shared._NF.Storage.Components;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Content.Shared.Item;
using Content.Shared.Verbs;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Content.Shared.Materials;
using Content.Shared.Power.EntitySystems;

namespace Content.Shared._NF.Storage.EntitySystems;

/// <summary>
/// Unified magnet pickup system that handles all magnet types.
/// Replaces the separate systems for regular storage, material storage, and material reclaimer magnets.
/// </summary>
public sealed class NFMagnetPickupSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ItemToggleSystem _itemToggleSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly SharedMaterialStorageSystem _materialStorage = default!;
    [Dependency] private readonly SharedMaterialReclaimerSystem _materialReclaimer = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _powerReceiver = default!;

    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<MaterialComponent> _materialQuery;
    private EntityQuery<MaterialStorageComponent> _materialStorageQuery;

    private const int MaxEntitiesToInsert = 15;

    public override void Initialize()
    {
        base.Initialize();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _materialQuery = GetEntityQuery<MaterialComponent>();
        _materialStorageQuery = GetEntityQuery<MaterialStorageComponent>();

        SubscribeLocalEvent<NFMagnetPickupComponent, MapInitEvent>(OnMagnetMapInit);
        SubscribeLocalEvent<NFMagnetPickupComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<NFMagnetPickupComponent, GetVerbsEvent<AlternativeVerb>>(AddToggleMagnetVerb);
        SubscribeLocalEvent<NFMagnetPickupComponent, ItemToggledEvent>(OnItemToggled);
    }

    private void OnMagnetMapInit(EntityUid uid, NFMagnetPickupComponent component, MapInitEvent args)
    {
        component.NextScan = _timing.CurTime + TimeSpan.FromSeconds(1);
        component.LastSuccessfulPickup = _timing.CurTime;

        // Always-on magnets should be enabled and cannot be turned off
        if (component.AlwaysOn)
        {
            component.MagnetEnabled = true;
            component.AutoDisableEnabled = false;
        }

        // Sync initial state with ItemToggleComponent if present
        if (TryComp<ItemToggleComponent>(uid, out var itemToggle))
        {
            _itemToggleSystem.TrySetActive((uid, itemToggle), component.MagnetEnabled);
        }

        // Update initial appearance
        UpdateMagnetAppearance(uid, component);
    }

    private void AddToggleMagnetVerb(EntityUid uid, NFMagnetPickupComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!HasComp<HandsComponent>(args.User))
            return;

        // Don't add toggle verb for always-on magnets
        if (component.AlwaysOn)
            return;

        // If the entity has ItemToggleComponent with alt use enabled, don't add our verb
        if (TryComp<ItemToggleComponent>(uid, out var toggleComp) && toggleComp.OnAltUse)
        {
            return;
        }

        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                ToggleMagnet(uid, component);
            },
            Icon = new SpriteSpecifier.Texture(new(NFMagnetPickupComponent.PowerToggleIconPath)),
            Text = Loc.GetString(NFMagnetPickupComponent.VerbToggleText),
            Priority = component.MagnetTogglePriority
        };

        args.Verbs.Add(verb);
    }

    private void OnExamined(EntityUid uid, NFMagnetPickupComponent component, ExaminedEvent args)
    {
        // Only show examination text for non-Storage magnet types
        if (component.PickupType == MagnetPickupType.Storage)
            return;

        if (!component.AlwaysOn)
        {
            // Show magnet status - use power-like text that already exists
            args.PushMarkup(Loc.GetString(NFMagnetPickupComponent.ExamineText,
                            ("stateText", Loc.GetString(component.MagnetEnabled
                            ? NFMagnetPickupComponent.ExamineTextOn
                            : NFMagnetPickupComponent.ExamineTextOff))));
        }
        else
        {
            args.PushMarkup(Loc.GetString(NFMagnetPickupComponent.ExamineText,
                            ("stateText", Loc.GetString(component.MagnetEnabled
                            ? NFMagnetPickupComponent.ExamineTextAlwaysOn
                            : NFMagnetPickupComponent.ExamineTextAlwaysOff))));
        }
    }

    private void OnItemToggled(EntityUid uid, NFMagnetPickupComponent component, ref ItemToggledEvent args)
    {
        // Don't process if this is an always-on magnet
        if (component.AlwaysOn)
            return;

        // Update magnet state to match ItemToggle state
        component.MagnetEnabled = args.Activated;

        // Reset auto-disable timer when manually toggling the magnet on (only if auto-disable is enabled)
        if (component.MagnetEnabled && component.AutoDisableEnabled)
        {
            component.LastSuccessfulPickup = _timing.CurTime;
        }

        // Update visual appearance
        UpdateMagnetAppearance(uid, component);
        Dirty(uid, component);
    }

    /// <summary>
    /// Toggles the magnet on the entity
    /// </summary>
    public void ToggleMagnet(EntityUid uid, NFMagnetPickupComponent comp)
    {
        // Always-on magnets cannot be toggled
        if (comp.AlwaysOn)
        {
            return;
        }

        var newState = !comp.MagnetEnabled;
        comp.MagnetEnabled = newState;

        // Reset auto-disable timer when manually toggling the magnet on (only if auto-disable is enabled)
        if (comp.MagnetEnabled && comp.AutoDisableEnabled)
        {
            comp.LastSuccessfulPickup = _timing.CurTime;
        }

        // Sync with ItemToggleComponent if present
        if (TryComp<ItemToggleComponent>(uid, out var itemToggle))
        {
            // Only sync if the ItemToggle state doesn't match our desired state
            if (itemToggle.Activated != comp.MagnetEnabled)
            {
                _itemToggleSystem.TrySetActive((uid, itemToggle), comp.MagnetEnabled);
            }
        }

        // Update visual appearance
        UpdateMagnetAppearance(uid, comp);
        Dirty(uid, comp);
    }

    /// <summary>
    /// Updates the magnet's visual appearance based on its current state
    /// </summary>
    private void UpdateMagnetAppearance(EntityUid uid, NFMagnetPickupComponent component)
    {
        var fillLevel = component.MagnetEnabled ? 1 : 0;
        _appearance.SetData(uid, StorageFillVisuals.FillLevel, fillLevel);
    }

    /// <summary>
    /// Updates the last successful pickup time and handles auto-disable logic
    /// </summary>
    private void HandleSuccessfulPickup(EntityUid uid, NFMagnetPickupComponent component, int successfulPickups)
    {
        if (successfulPickups > 0)
        {
            component.LastSuccessfulPickup = _timing.CurTime;
        }
    }

    /// <summary>
    /// Checks if the magnet should be auto-disabled and disables it if necessary
    /// </summary>
    private bool CheckAutoDisable(EntityUid uid, NFMagnetPickupComponent component)
    {
        // Always-on magnets never auto-disable
        if (component.AlwaysOn)
        {
            return false;
        }

        // Early return if auto-disable is not enabled
        if (!component.AutoDisableEnabled)
        {
            return false;
        }

        if (!component.MagnetEnabled)
        {
            return false;
        }

        var currentTime = _timing.CurTime;

        // Initialize LastSuccessfulPickup if this is the first auto-disable check
        if (component.LastSuccessfulPickup == TimeSpan.Zero)
        {
            component.LastSuccessfulPickup = currentTime;
            return false;
        }

        // Reset if LastSuccessfulPickup is in the future (shouldn't happen)
        if (component.LastSuccessfulPickup > currentTime)
        {
            component.LastSuccessfulPickup = currentTime;
            return false;
        }

        var timeSinceLastPickup = currentTime - component.LastSuccessfulPickup;

        if (timeSinceLastPickup >= component.AutoDisableTime)
        {
            component.MagnetEnabled = false;

            // Sync with ItemToggleComponent if present
            if (TryComp<ItemToggleComponent>(uid, out var itemToggle))
            {
                _itemToggleSystem.TrySetActive((uid, itemToggle), false);
            }

            // Update visual appearance when auto-disabled
            UpdateMagnetAppearance(uid, component);

            return true;
        }

        return false;
    }

    /// <summary>
    /// Common logic for checking if an entity should be processed
    /// </summary>
    private bool ShouldProcessEntity(EntityUid near, EntityUid parentUid, PhysicsComponent? physics = null)
    {
        if (near == parentUid)
            return false;

        // Allow entities that are on ground or don't have physics
        if (_physicsQuery.TryGetComponent(near, out physics))
        {
            // Only exclude entities that are explicitly in the air
            if (physics.BodyStatus == BodyStatus.InAir)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Calculate the next scan delay based on activity
    /// </summary>
    private TimeSpan CalculateNextScanDelay(int successfulPickups, bool foundTargets)
    {
        return successfulPickups > 0 ? NFMagnetPickupComponent.FastScanDelay :
               foundTargets ? NFMagnetPickupComponent.ScanDelay :
               NFMagnetPickupComponent.SlowScanDelay;
    }

    /// <summary>
    /// Processes regular storage magnet pickup
    /// </summary>
    private (int successfulPickups, bool foundTargets) ProcessStorageMagnet(EntityUid uid, NFMagnetPickupComponent comp, StorageComponent storage, TransformComponent xform, MetaDataComponent meta)
    {
        var slotCount = _storage.GetCumulativeItemAreas((uid, storage));
        var totalSlots = storage.Grid.GetArea();
        if (slotCount >= totalSlots)
            return (0, false);

        var parentUid = xform.ParentUid;
        var finalCoords = xform.Coordinates;
        var moverCoords = _transform.GetMoverCoordinates(uid, xform);
        var count = 0;
        var successfulPickups = 0;
        var foundTargets = false;

        foreach (var near in _lookup.GetEntitiesInRange(uid, comp.Range, LookupFlags.Dynamic | LookupFlags.Sundries))
        {
            if (count >= MaxEntitiesToInsert)
                break;

            if (!ShouldProcessEntity(near, parentUid))
                continue;

            foundTargets = true;

            if (_whitelistSystem.IsWhitelistFail(storage.Whitelist, near))
                continue;

            if (!TryComp<ItemComponent>(near, out var item))
                continue;

            var itemSize = _item.GetItemShape((near, item)).GetArea();
            if (itemSize > totalSlots - slotCount)
                break;

            // Count only objects we _could_ insert.
            count++;

            if (!_storage.Insert(uid, near, out var stacked, storageComp: storage, playSound: false))
                break;

            successfulPickups++;
            slotCount += itemSize;
        }

        return (successfulPickups, foundTargets);
    }

    /// <summary>
    /// Processes material storage magnet pickup
    /// </summary>
    private (int successfulPickups, bool foundTargets) ProcessMaterialStorageMagnet(EntityUid uid, NFMagnetPickupComponent comp, MaterialStorageComponent storage, TransformComponent xform)
    {
        // Check if powered - material storage typically requires power
        if (!_powerReceiver.IsPowered((uid, null)))
            return (0, false);

        // Early termination: Skip if storage is at capacity
        if (storage.MaterialWhiteList != null && storage.MaterialWhiteList.Count > 0)
        {
            bool hasSpace = false;
            foreach (var material in storage.MaterialWhiteList)
            {
                if (_materialStorage.CanChangeMaterialAmount(uid, material, 1, storage))
                {
                    hasSpace = true;
                    break;
                }
            }

            if (!hasSpace)
                return (0, false);
        }

        var parentUid = xform.ParentUid;
        var entitiesProcessed = 0;
        var successfulPickups = 0;
        var foundMaterials = false;

        foreach (var near in _lookup.GetEntitiesInRange(uid, comp.Range, LookupFlags.Dynamic | LookupFlags.Sundries))
        {
            if (entitiesProcessed >= NFMagnetPickupComponent.MaxEntitiesPerScan)
                break;

            entitiesProcessed++;

            if (!ShouldProcessEntity(near, parentUid))
                continue;

            if (!_materialQuery.HasComponent(near))
                continue;

            foundMaterials = true;

            if (_materialStorage.TryInsertMaterialEntity(uid, near, uid, storage))
            {
                successfulPickups++;

                if (successfulPickups >= NFMagnetPickupComponent.MaxPickupsPerScan)
                    break;
            }
        }

        return (successfulPickups, foundMaterials);
    }

    /// <summary>
    /// Processes material reclaimer magnet pickup
    /// </summary>
    private (int successfulPickups, bool foundTargets) ProcessMaterialReclaimerMagnet(EntityUid uid, NFMagnetPickupComponent comp, MaterialReclaimerComponent storage, TransformComponent xform)
    {
        // Check if powered - material reclaimer typically requires power
        if (!_powerReceiver.IsPowered((uid, null)))
            return (0, false);

        // Check if the reclaimer can start processing - this handles enabled state, broken state, etc.
        if (!_materialReclaimer.CanStart(uid, storage))
            return (0, false);

        var parentUid = xform.ParentUid;
        var entitiesProcessed = 0;
        var successfulPickups = 0;
        var foundTargets = false;

        foreach (var near in _lookup.GetEntitiesInRange(uid, comp.Range, LookupFlags.Dynamic | LookupFlags.Sundries))
        {
            if (entitiesProcessed >= NFMagnetPickupComponent.MaxEntitiesPerScan)
                break;

            entitiesProcessed++;

            if (!ShouldProcessEntity(near, parentUid))
                continue;

            foundTargets = true;

            if (_materialReclaimer.TryStartProcessItem(uid, near))
            {
                successfulPickups++;

                if (successfulPickups >= NFMagnetPickupComponent.MaxPickupsPerScan)
                    break;
            }
        }

        return (successfulPickups, foundTargets);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<NFMagnetPickupComponent, TransformComponent>();
        var currentTime = _timing.CurTime;

        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (comp.NextScan > currentTime)
                continue;

            comp.NextScan = currentTime + NFMagnetPickupComponent.ScanDelay;

            // Check auto-disable before processing
            if (CheckAutoDisable(uid, comp))
            {
                Dirty(uid, comp); // Mark as dirty if auto-disabled
                continue;
            }

            if (!comp.MagnetEnabled)
            {
                continue;
            }

            var successfulPickups = 0;
            var foundTargets = false;

            // Process based on magnet type
            switch (comp.PickupType)
            {
                case MagnetPickupType.Storage:
                    if (TryComp<StorageComponent>(uid, out var storage) && TryComp<MetaDataComponent>(uid, out var meta))
                    {
                        (successfulPickups, foundTargets) = ProcessStorageMagnet(uid, comp, storage, xform, meta);
                    }
                    break;

                case MagnetPickupType.MaterialStorage:
                    if (TryComp<MaterialStorageComponent>(uid, out var materialStorage))
                    {
                        (successfulPickups, foundTargets) = ProcessMaterialStorageMagnet(uid, comp, materialStorage, xform);
                    }
                    break;

                case MagnetPickupType.MaterialReclaimer:
                    if (TryComp<MaterialReclaimerComponent>(uid, out var materialReclaimer))
                    {
                        (successfulPickups, foundTargets) = ProcessMaterialReclaimerMagnet(uid, comp, materialReclaimer, xform);
                    }
                    break;
            }

            // Handle successful pickup tracking for auto-disable
            HandleSuccessfulPickup(uid, comp, successfulPickups);

            comp.NextScan = currentTime + CalculateNextScanDelay(successfulPickups, foundTargets);
        }
    }
}
