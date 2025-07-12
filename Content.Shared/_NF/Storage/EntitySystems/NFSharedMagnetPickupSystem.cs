using Content.Shared._NF.Storage.Components;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Item;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Materials;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._NF.Storage.EntitySystems;

/// <summary>
/// Unified magnet pickup system that handles all magnet types.
/// Replaces the separate systems for regular storage, material storage, and material reclaimer magnets.
/// </summary>
public sealed class NFSharedMagnetPickupSystem : EntitySystem
{
    #region Dependencies

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

    #endregion

    #region Entity Queries

    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<MaterialComponent> _materialQuery;

    #endregion

    #region Constants

    /// <summary>
    /// Maximum entities to attempt insertion per scan (performance limit)
    /// </summary>
    private const int MaxEntitiesToInsert = 15;

    #endregion

    #region Initialization

    public override void Initialize()
    {
        base.Initialize();

        // Initialize entity queries for performance
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _materialQuery = GetEntityQuery<MaterialComponent>();

        // Subscribe to events
        SubscribeLocalEvent<NFMagnetPickupComponent, MapInitEvent>(OnMagnetMapInit);
        SubscribeLocalEvent<NFMagnetPickupComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<NFMagnetPickupComponent, GetVerbsEvent<AlternativeVerb>>(AddToggleMagnetVerb);
        SubscribeLocalEvent<NFMagnetPickupComponent, ItemToggledEvent>(OnItemToggled);
    }

    #endregion

    #region Event Handlers

    private void OnMagnetMapInit(EntityUid uid, NFMagnetPickupComponent component, MapInitEvent args)
    {
        // Initialize timing
        component.NextScan = _timing.CurTime + TimeSpan.FromSeconds(1);
        component.LastSuccessfulPickup = _timing.CurTime;

        // Configure always-on magnets
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
        // Validate user can interact
        if (!args.CanAccess || !args.CanInteract || !HasComp<HandsComponent>(args.User))
            return;

        // Don't add toggle verb for always-on magnets
        if (component.AlwaysOn)
            return;

        // Don't add our verb if ItemToggleComponent handles alt-use
        if (TryComp<ItemToggleComponent>(uid, out var toggleComp) && toggleComp.OnAltUse)
            return;

        var verb = new AlternativeVerb
        {
            Act = () => ToggleMagnet(uid, component),
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

        var stateKey = component.AlwaysOn
            ? (component.MagnetEnabled ? NFMagnetPickupComponent.ExamineTextAlwaysOn : NFMagnetPickupComponent.ExamineTextAlwaysOff)
            : (component.MagnetEnabled ? NFMagnetPickupComponent.ExamineTextOn : NFMagnetPickupComponent.ExamineTextOff);

        args.PushMarkup(Loc.GetString(NFMagnetPickupComponent.ExamineText, ("stateText", Loc.GetString(stateKey))));
    }

    private void OnItemToggled(EntityUid uid, NFMagnetPickupComponent component, ref ItemToggledEvent args)
    {
        // Don't process if this is an always-on magnet
        if (component.AlwaysOn)
            return;

        // Update magnet state to match ItemToggle state
        component.MagnetEnabled = args.Activated;

        // Reset auto-disable timer when manually toggling the magnet on
        if (component.MagnetEnabled && component.AutoDisableEnabled)
        {
            component.LastSuccessfulPickup = _timing.CurTime;
        }

        // Update visual appearance and mark dirty
        UpdateMagnetAppearance(uid, component);
        Dirty(uid, component);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Toggles the magnet state for the specified entity
    /// </summary>
    /// <param name="uid">Entity with magnet component</param>
    /// <param name="comp">Magnet component</param>
    public void ToggleMagnet(EntityUid uid, NFMagnetPickupComponent comp)
    {
        // Always-on magnets cannot be toggled
        if (comp.AlwaysOn)
            return;

        var newState = !comp.MagnetEnabled;
        comp.MagnetEnabled = newState;

        // Reset auto-disable timer when manually toggling the magnet on
        if (comp.MagnetEnabled && comp.AutoDisableEnabled)
        {
            comp.LastSuccessfulPickup = _timing.CurTime;
        }

        // Sync with ItemToggleComponent if present
        if (TryComp<ItemToggleComponent>(uid, out var itemToggle) && itemToggle.Activated != comp.MagnetEnabled)
        {
            _itemToggleSystem.TrySetActive((uid, itemToggle), comp.MagnetEnabled);
        }

        // Update visual appearance and mark dirty
        UpdateMagnetAppearance(uid, comp);
        Dirty(uid, comp);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Updates the magnet's visual appearance based on its current state
    /// </summary>
    private void UpdateMagnetAppearance(EntityUid uid, NFMagnetPickupComponent component)
    {
        var fillLevel = component.MagnetEnabled ? 1 : 0;
        _appearance.SetData(uid, StorageFillVisuals.FillLevel, fillLevel);
    }

    /// <summary>
    /// Updates the last successful pickup time for auto-disable tracking
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
    /// <returns>True if the magnet was auto-disabled</returns>
    private bool CheckAutoDisable(EntityUid uid, NFMagnetPickupComponent component)
    {
        // Early returns for conditions that prevent auto-disable
        if (component.AlwaysOn || !component.AutoDisableEnabled || !component.MagnetEnabled)
            return false;

        var currentTime = _timing.CurTime;

        // Initialize LastSuccessfulPickup if this is the first check
        if (component.LastSuccessfulPickup == TimeSpan.Zero)
        {
            component.LastSuccessfulPickup = currentTime;
            return false;
        }

        // Defensive programming: reset if timestamp is in the future
        if (component.LastSuccessfulPickup > currentTime)
        {
            component.LastSuccessfulPickup = currentTime;
            return false;
        }

        // Check if auto-disable time has elapsed
        var timeSinceLastPickup = currentTime - component.LastSuccessfulPickup;
        if (timeSinceLastPickup < component.AutoDisableTime)
            return false;

        // Auto-disable the magnet
        component.MagnetEnabled = false;

        // Sync with ItemToggleComponent if present
        if (TryComp<ItemToggleComponent>(uid, out var itemToggle))
        {
            _itemToggleSystem.TrySetActive((uid, itemToggle), false);
        }

        // Update appearance
        UpdateMagnetAppearance(uid, component);
        return true;
    }

    /// <summary>
    /// Determines if an entity should be processed for pickup
    /// </summary>
    private bool ShouldProcessEntity(EntityUid near, EntityUid parentUid)
    {
        // Don't process self
        if (near == parentUid)
            return false;

        // Check physics status if component exists
        if (_physicsQuery.TryGetComponent(near, out var physics))
        {
            // Only exclude entities that are explicitly in the air
            return physics.BodyStatus != BodyStatus.InAir;
        }

        // Allow entities without physics components
        return true;
    }

    /// <summary>
    /// Calculates the next scan delay based on pickup activity
    /// </summary>
    private static TimeSpan CalculateNextScanDelay(int successfulPickups, bool foundTargets)
    {
        return successfulPickups > 0 ? NFMagnetPickupComponent.FastScanDelay :
               foundTargets ? NFMagnetPickupComponent.ScanDelay :
               NFMagnetPickupComponent.SlowScanDelay;
    }

    #endregion

    #region Magnet Type Processors

    /// <summary>
    /// Processes regular storage magnet pickup
    /// </summary>
    private (int successfulPickups, bool foundTargets) ProcessStorageMagnet(
        EntityUid uid,
        NFMagnetPickupComponent comp,
        StorageComponent storage,
        TransformComponent xform,
        MetaDataComponent meta)
    {
        var slotCount = _storage.GetCumulativeItemAreas((uid, storage));
        var totalSlots = storage.Grid.GetArea();

        // Early return if storage is full
        if (slotCount >= totalSlots)
            return (0, false);

        var parentUid = xform.ParentUid;
        var finalCoords = xform.Coordinates;
        var moverCoords = _transform.GetMoverCoordinates(uid, xform);
        var count = 0;
        var successfulPickups = 0;
        var foundTargets = false;
        var playedSound = false;

        foreach (var near in _lookup.GetEntitiesInRange(uid, comp.Range, LookupFlags.Dynamic | LookupFlags.Sundries))
        {
            if (count >= MaxEntitiesToInsert)
                break;

            if (!ShouldProcessEntity(near, parentUid))
                continue;

            foundTargets = true;

            // Check whitelist
            if (_whitelistSystem.IsWhitelistFail(storage.Whitelist, near))
                continue;

            // Must be an item
            if (!TryComp<ItemComponent>(near, out var item))
                continue;

            // Check if item fits
            var itemSize = _item.GetItemShape((near, item)).GetArea();
            if (itemSize > totalSlots - slotCount)
                break;

            count++;

            // Capture position and rotation before insertion for animation
            var nearXform = Transform(near);
            var nearMap = _transform.GetMapCoordinates(near, xform: nearXform);
            var nearCoords = _transform.ToCoordinates(moverCoords.EntityId, nearMap);

            // Attempt insertion
            if (!_storage.Insert(uid, near, out var stacked, storageComp: storage, playSound: !playedSound))
                break;

            // Play pickup animation (same pattern as original MagnetPickupSystem)
            var animatedEntity = stacked ?? near;
            _storage.PlayPickupAnimation(animatedEntity, nearCoords, finalCoords, nearXform.LocalRotation);

            successfulPickups++;
            slotCount += itemSize;
            playedSound = true;
        }

        return (successfulPickups, foundTargets);
    }

    /// <summary>
    /// Processes material storage magnet pickup
    /// </summary>
    private (int successfulPickups, bool foundTargets) ProcessMaterialStorageMagnet(
        EntityUid uid,
        NFMagnetPickupComponent comp,
        MaterialStorageComponent storage,
        TransformComponent xform)
    {
        // Check power requirements
        if (!_powerReceiver.IsPowered((uid, null)))
            return (0, false);

        // Check if storage has capacity
        if (storage.MaterialWhiteList != null && storage.MaterialWhiteList.Count > 0)
        {
            var hasSpace = false;
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
    private (int successfulPickups, bool foundTargets) ProcessMaterialReclaimerMagnet(
        EntityUid uid,
        NFMagnetPickupComponent comp,
        MaterialReclaimerComponent storage,
        TransformComponent xform)
    {
        // Check power and operational state
        if (!_powerReceiver.IsPowered((uid, null)) || !_materialReclaimer.CanStart(uid, storage))
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

    #endregion

    #region Update Loop

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NFMagnetPickupComponent, TransformComponent>();
        var currentTime = _timing.CurTime;

        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            // Skip if not time to scan yet
            if (comp.NextScan > currentTime)
                continue;

            comp.NextScan = currentTime + NFMagnetPickupComponent.ScanDelay;

            // Check auto-disable before processing
            if (CheckAutoDisable(uid, comp))
            {
                Dirty(uid, comp);
                continue;
            }

            // Skip if magnet is disabled
            if (!comp.MagnetEnabled)
                continue;

            var successfulPickups = 0;
            var foundTargets = false;

            // Process based on magnet type
            switch (comp.PickupType)
            {
                case MagnetPickupType.Storage:
                    if (TryComp<StorageComponent>(uid, out var storage) && TryComp(uid, out MetaDataComponent? meta))
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

            // Update timing for auto-disable tracking
            HandleSuccessfulPickup(uid, comp, successfulPickups);

            // Set next scan time based on activity
            comp.NextScan = currentTime + CalculateNextScanDelay(successfulPickups, foundTargets);
        }
    }

    #endregion
}
