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
using Content.Shared.Storage.Components; // Added for StorageFillVisuals
using Robust.Shared.GameObjects;

namespace Content.Shared._NF.Storage.EntitySystems;

/// <summary>
/// Base system for all magnet pickup functionality.
/// Provides common functionality for regular, material storage, and material reclaimer magnets.
/// </summary>
public abstract class BaseMagnetPickupSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly EntityLookupSystem Lookup = default!;
    [Dependency] protected readonly new SharedTransformSystem Transform = default!;
    [Dependency] protected readonly ItemToggleSystem ItemToggleSystem = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!; // Added for visual updates

    protected EntityQuery<PhysicsComponent> PhysicsQuery;

    public override void Initialize()
    {
        base.Initialize();
        PhysicsQuery = GetEntityQuery<PhysicsComponent>();
    }

    /// <summary>
    /// Common logic for handling magnet map initialization
    /// </summary>
    protected void HandleMagnetMapInit<T>(EntityUid uid, T component, MapInitEvent args)
        where T : IBaseMagnetPickupComponent
    {
        component.NextScan = Timing.CurTime + TimeSpan.FromSeconds(1);
        // Always initialize LastSuccessfulPickup to current time to prevent immediate auto-disable
        component.LastSuccessfulPickup = Timing.CurTime;
        
        // Always-on magnets should be enabled and cannot be turned off
        if (component.AlwaysOn)
        {
            component.MagnetEnabled = true;
            // Disable auto-disable for always-on magnets
            component.AutoDisableEnabled = false;
        }
        
        // Sync initial state with ItemToggleComponent if present
        if (TryComp<ItemToggleComponent>(uid, out var itemToggle))
        {
            ItemToggleSystem.TrySetActive((uid, itemToggle), component.MagnetEnabled);
        }
        
        // Update initial appearance
        UpdateMagnetAppearance(uid, component);
        
        Log.Debug($"Magnet {uid} initialized with state: MagnetEnabled={component.MagnetEnabled}, AutoDisable={component.AutoDisableEnabled}, AlwaysOn={component.AlwaysOn}");
    }

    /// <summary>
    /// Common logic for handling entity unpaused events
    /// </summary>
    protected void HandleMagnetUnpaused<T>(EntityUid uid, T component, ref EntityUnpausedEvent args)
        where T : IBaseMagnetPickupComponent
    {
        component.NextScan += args.PausedTime;
        // Adjust auto-disable timing for pause only if it's been initialized
        if (component.AutoDisableEnabled && component.LastSuccessfulPickup != TimeSpan.Zero)
        {
            component.LastSuccessfulPickup += args.PausedTime;
        }
    }

    /// <summary>
    /// Common logic for adding toggle magnet verbs
    /// </summary>
    protected void HandleAddToggleMagnetVerb<T>(EntityUid uid, T component, GetVerbsEvent<AlternativeVerb> args, int? priority = null)
        where T : IBaseMagnetPickupComponent
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!HasComp<HandsComponent>(args.User))
            return;

        // Don't add toggle verb for always-on magnets
        if (component.AlwaysOn)
            return;

        // If the entity has ItemToggleComponent with alt use enabled, don't add our verb
        // The ItemToggleComponent will handle the verb and we'll sync via events
        if (TryComp<ItemToggleComponent>(uid, out var toggleComp) && toggleComp.OnAltUse)
        {
            return; // Let ItemToggleComponent handle the verb
        }

        var verbPriority = priority ?? component.MagnetTogglePriority;

        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                ToggleMagnet(uid, component);
            },
            Icon = new SpriteSpecifier.Texture(new(NFMagnetPickupComponent.PowerToggleIconPath)),
            Text = Loc.GetString(NFMagnetPickupComponent.VerbToggleText),
            Priority = verbPriority
        };

        args.Verbs.Add(verb);
    }

    /// <summary>
    /// Common logic for examination events
    /// </summary>
    protected void HandleExamined<T>(EntityUid uid, T component, ExaminedEvent args)
        where T : IBaseMagnetPickupComponent
    {
        var stateKey = component.MagnetEnabled
            ? NFMagnetPickupComponent.ExamineStateEnabled
            : NFMagnetPickupComponent.ExamineStateDisabled;

        args.PushMarkup(Loc.GetString(NFMagnetPickupComponent.ExamineText,
            ("state", Loc.GetString(stateKey))));

        // Add additional info for always-on magnets
        if (component.AlwaysOn)
        {
            args.PushMarkup(Loc.GetString("magnet-pickup-examine-always-on"));
        }
    }

    /// <summary>
    /// Common logic for toggling magnets
    /// </summary>
    protected bool ToggleMagnet<T>(EntityUid uid, T comp)
        where T : IBaseMagnetPickupComponent
    {
        // Always-on magnets cannot be toggled
        if (comp.AlwaysOn)
        {
            Log.Warning($"Attempted to toggle always-on magnet {uid}, ignoring");
            return comp.MagnetEnabled;
        }

        var newState = !comp.MagnetEnabled;
        Log.Debug($"Magnet {uid} toggle: {comp.MagnetEnabled} -> {newState}");
        
        comp.MagnetEnabled = newState;

        // Reset auto-disable timer when manually toggling the magnet on (only if auto-disable is enabled)
        if (comp.MagnetEnabled && comp.AutoDisableEnabled)
        {
            comp.LastSuccessfulPickup = Timing.CurTime;
        }

        // Sync with ItemToggleComponent if present - but don't let it override our state
        if (TryComp<ItemToggleComponent>(uid, out var itemToggle))
        {
            // Only sync if the ItemToggle state doesn't match our desired state
            if (itemToggle.Activated != comp.MagnetEnabled)
            {
                var result = ItemToggleSystem.TrySetActive((uid, itemToggle), comp.MagnetEnabled);
                if (!result)
                {
                    Log.Warning($"Failed to sync ItemToggleComponent state for magnet {uid}. Magnet: {comp.MagnetEnabled}, ItemToggle: {itemToggle.Activated}");
                }
            }
        }

        // Update visual appearance
        UpdateMagnetAppearance(uid, comp);

        Log.Debug($"Magnet {uid} state after toggle: {comp.MagnetEnabled}");
        return comp.MagnetEnabled;
    }

    /// <summary>
    /// Updates the magnet's visual appearance based on its current state
    /// </summary>
    protected void UpdateMagnetAppearance<T>(EntityUid uid, T component)
        where T : IBaseMagnetPickupComponent
    {
        // Use the magnet enabled state as the fill level for visual consistency
        // 0 = disabled/off, 1 = enabled/on
        var fillLevel = component.MagnetEnabled ? 1 : 0;
        Appearance.SetData(uid, StorageFillVisuals.FillLevel, fillLevel);
    }

    /// <summary>
    /// Updates the last successful pickup time and handles auto-disable logic
    /// </summary>
    protected void HandleSuccessfulPickup<T>(EntityUid uid, T component, int successfulPickups)
        where T : IBaseMagnetPickupComponent
    {
        // Always update LastSuccessfulPickup when there are successful pickups,
        // regardless of AutoDisableEnabled state, so the timer is ready if auto-disable gets enabled later
        if (successfulPickups > 0)
        {
            component.LastSuccessfulPickup = Timing.CurTime;
            if (component.AutoDisableEnabled)
            {
                Log.Debug($"Magnet {uid} auto-disable timer reset due to {successfulPickups} successful pickups");
            }
        }
    }

    /// <summary>
    /// Checks if the magnet should be auto-disabled and disables it if necessary
    /// </summary>
    protected bool CheckAutoDisable<T>(EntityUid uid, T component)
        where T : IBaseMagnetPickupComponent
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
            // Magnet is already disabled
            return false;
        }

        var currentTime = Timing.CurTime;

        // If LastSuccessfulPickup is zero, it means no pickups have occurred since initialization
        // Initialize it to current time ONLY if this is the first time auto-disable is being checked
        if (component.LastSuccessfulPickup == TimeSpan.Zero)
        {
            component.LastSuccessfulPickup = currentTime;
            Log.Debug($"Magnet {uid} auto-disable timer initialized");
            return false; // Don't auto-disable on first check, give it a chance
        }

        // If LastSuccessfulPickup is in the future (shouldn't happen), reset it
        if (component.LastSuccessfulPickup > currentTime)
        {
            Log.Warning($"Magnet {uid} LastSuccessfulPickup is in the future, resetting");
            component.LastSuccessfulPickup = currentTime;
            return false;
        }

        var timeSinceLastPickup = currentTime - component.LastSuccessfulPickup;

        // Debug: Log when we're close to auto-disable (for testing purposes)
        if (timeSinceLastPickup >= component.AutoDisableTime * 0.9)
        {
            var remaining = component.AutoDisableTime - timeSinceLastPickup;
            Log.Debug($"Magnet {uid} auto-disable in {remaining.TotalSeconds:F1} seconds");
        }

        if (timeSinceLastPickup >= component.AutoDisableTime)
        {
            Log.Info($"Magnet {uid} auto-disabled after {timeSinceLastPickup.TotalSeconds:F1} seconds of inactivity");
            component.MagnetEnabled = false;
            
            // Sync with ItemToggleComponent if present
            if (TryComp<ItemToggleComponent>(uid, out var itemToggle))
            {
                var result = ItemToggleSystem.TrySetActive((uid, itemToggle), false);
                if (!result)
                {
                    Log.Warning($"Failed to sync ItemToggleComponent state for auto-disabled magnet {uid}");
                }
            }
            
            // Update visual appearance when auto-disabled
            UpdateMagnetAppearance(uid, component);
            
            return true; // Magnet was auto-disabled
        }

        return false; // Magnet remains enabled
    }

    /// <summary>
    /// Common logic for checking if an entity should be processed
    /// </summary>
    protected bool ShouldProcessEntity(EntityUid near, EntityUid parentUid, PhysicsComponent? physics = null)
    {
        if (near == parentUid)
            return false;

        // More permissive physics check - allow entities that are on ground or don't have physics
        if (PhysicsQuery.TryGetComponent(near, out physics))
        {
            // Only exclude entities that are explicitly in the air or have other incompatible status
            if (physics.BodyStatus == BodyStatus.InAir)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Calculate the next scan delay based on activity
    /// </summary>
    protected TimeSpan CalculateNextScanDelay(int successfulPickups, bool foundTargets)
    {
        return successfulPickups > 0 ? NFMagnetPickupComponent.FastScanDelay :
               foundTargets ? NFMagnetPickupComponent.ScanDelay :
               NFMagnetPickupComponent.SlowScanDelay;
    }

    /// <summary>
    /// Common logic for handling ItemToggle state changes
    /// </summary>
    protected void HandleItemToggled<T>(EntityUid uid, T component, ref ItemToggledEvent args)
        where T : IBaseMagnetPickupComponent
    {
        // Don't process if this is an always-on magnet
        if (component.AlwaysOn)
            return;

        var newState = args.Activated;
        Log.Debug($"Magnet {uid} ItemToggle changed: {component.MagnetEnabled} -> {newState}");
        
        // Update magnet state to match ItemToggle state
        component.MagnetEnabled = newState;

        // Reset auto-disable timer when manually toggling the magnet on (only if auto-disable is enabled)
        if (component.MagnetEnabled && component.AutoDisableEnabled)
        {
            component.LastSuccessfulPickup = Timing.CurTime;
        }

        // Update visual appearance
        UpdateMagnetAppearance(uid, component);

        Log.Debug($"Magnet {uid} state synchronized from ItemToggle: {component.MagnetEnabled}");
    }
}

/// <summary>
/// <see cref="NFMagnetPickupComponent"/>
/// </summary>
public sealed class NFMagnetPickupSystem : BaseMagnetPickupSystem
{
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;

    private const int MaxEntitiesToInsert = 15;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NFMagnetPickupComponent, MapInitEvent>(OnMagnetMapInit);
        SubscribeLocalEvent<NFMagnetPickupComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<NFMagnetPickupComponent, GetVerbsEvent<AlternativeVerb>>(AddToggleMagnetVerb);
        SubscribeLocalEvent<NFMagnetPickupComponent, ItemToggledEvent>(OnItemToggled);
    }

    private void OnMagnetMapInit(EntityUid uid, NFMagnetPickupComponent component, MapInitEvent args)
    {
        HandleMagnetMapInit(uid, component, args);
    }

    private void AddToggleMagnetVerb(EntityUid uid, NFMagnetPickupComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        HandleAddToggleMagnetVerb(uid, component, args, component.MagnetTogglePriority);
    }

    // Show the magnet state on examination
    private void OnExamined(EntityUid uid, NFMagnetPickupComponent component, ExaminedEvent args)
    {
        HandleExamined(uid, component, args);
    }

    private void OnItemToggled(EntityUid uid, NFMagnetPickupComponent component, ref ItemToggledEvent args)
    {
        HandleItemToggled(uid, component, ref args);
        Dirty(uid, component);
    }

    /// <summary>
    /// Toggles the magnet on the ore bag/box
    /// </summary>
    public void ToggleMagnet(EntityUid uid, NFMagnetPickupComponent comp)
    {
        ToggleMagnet<NFMagnetPickupComponent>(uid, comp);
        Dirty(uid, comp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<NFMagnetPickupComponent, StorageComponent, TransformComponent, MetaDataComponent>();
        var currentTime = Timing.CurTime;

        while (query.MoveNext(out var uid, out var comp, out var storage, out var xform, out var meta))
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

            var slotCount = _storage.GetCumulativeItemAreas((uid, storage));
            var totalSlots = storage.Grid.GetArea();
            if (slotCount >= totalSlots)
                continue;

            var parentUid = xform.ParentUid;
            var playedSound = false;
            var finalCoords = xform.Coordinates;
            var moverCoords = Transform.GetMoverCoordinates(uid, xform);
            var count = 0;
            var successfulPickups = 0;
            var foundTargets = false;

            foreach (var near in Lookup.GetEntitiesInRange(uid, comp.Range, LookupFlags.Dynamic | LookupFlags.Sundries))
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

                var nearXform = base.Transform(near); // Use base.Transform method to get TransformComponent
                var nearMap = Transform.GetMapCoordinates(near, xform: nearXform);
                var nearCoords = Transform.ToCoordinates(moverCoords.EntityId, nearMap);

                if (!_storage.Insert(uid, near, out var stacked, storageComp: storage, playSound: !playedSound))
                    break;

                slotCount += itemSize; // adjust size (assume it's in a new slot)
                successfulPickups++;

                // Play pickup animation for either the stack entity or the original entity.
                if (stacked != null)
                    _storage.PlayPickupAnimation(stacked.Value, nearCoords, finalCoords, nearXform.LocalRotation);
                else
                    _storage.PlayPickupAnimation(near, nearCoords, finalCoords, nearXform.LocalRotation);

                playedSound = true;
            }

            // Handle successful pickup tracking for auto-disable
            HandleSuccessfulPickup(uid, comp, successfulPickups);

            // Use dynamic scan delay based on activity
            comp.NextScan = currentTime + CalculateNextScanDelay(successfulPickups, foundTargets);
        }
    }
}
