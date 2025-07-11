using Content.Shared._NF.Storage.Components;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Whitelist;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Content.Shared.Item;
using Content.Shared.Verbs;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Storage;
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
    }

    /// <summary>
    /// Common logic for handling entity unpaused events
    /// </summary>
    protected void HandleMagnetUnpaused<T>(EntityUid uid, T component, ref EntityUnpausedEvent args)
        where T : IBaseMagnetPickupComponent
    {
        component.NextScan += args.PausedTime;
    }

    /// <summary>
    /// Common logic for adding toggle magnet verbs
    /// </summary>
    protected void HandleAddToggleMagnetVerb<T>(EntityUid uid, T component, GetVerbsEvent<AlternativeVerb> args, int priority = NFMagnetPickupComponent.DefaultVerbPriority)
        where T : IBaseMagnetPickupComponent
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!HasComp<HandsComponent>(args.User))
            return;

        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                ToggleMagnet(uid, component);
            },
            Icon = new SpriteSpecifier.Texture(new(NFMagnetPickupComponent.PowerToggleIconPath)),
            Text = Loc.GetString(NFMagnetPickupComponent.VerbToggleText),
            Priority = priority
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
    }

    /// <summary>
    /// Common logic for toggling magnets
    /// </summary>
    protected bool ToggleMagnet<T>(EntityUid uid, T comp)
        where T : IBaseMagnetPickupComponent
    {
        comp.MagnetEnabled = !comp.MagnetEnabled;
        return comp.MagnetEnabled;
    }

    /// <summary>
    /// Common logic for checking if an entity should be processed
    /// </summary>
    protected bool ShouldProcessEntity(EntityUid near, EntityUid parentUid, PhysicsComponent? physics = null)
    {
        if (near == parentUid)
            return false;

        if (!PhysicsQuery.TryGetComponent(near, out physics) || physics.BodyStatus != BodyStatus.OnGround)
            return false;

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
    }

    private void OnMagnetMapInit(EntityUid uid, NFMagnetPickupComponent component, MapInitEvent args)
    {
        component.NextScan = Timing.CurTime;
    }

    private void AddToggleMagnetVerb(EntityUid uid, NFMagnetPickupComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        // Magnet run by other means (e.g. toggles)
        if (!component.MagnetCanBeEnabled)
            return;

        HandleAddToggleMagnetVerb(uid, component, args, component.MagnetTogglePriority);
    }

    // Show the magnet state on examination
    private void OnExamined(EntityUid uid, NFMagnetPickupComponent component, ExaminedEvent args)
    {
        // Magnet run by other means (e.g. toggles)
        if (!component.MagnetCanBeEnabled)
            return;

        HandleExamined(uid, component, args);
    }

    /// <summary>
    /// Toggles the magnet on the ore bag/box
    /// </summary>
    public void ToggleMagnet(EntityUid uid, NFMagnetPickupComponent comp)
    {
        // Magnet run by other means (e.g. toggles)
        if (!comp.MagnetCanBeEnabled)
            return;

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

            if (comp.MagnetCanBeEnabled)
            {
                if (!comp.MagnetEnabled)
                    continue;
            }
            else
            {
                if (!_toggle.IsActivated(uid))
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

            // Use dynamic scan delay based on activity
            comp.NextScan = currentTime + CalculateNextScanDelay(successfulPickups, foundTargets);
        }
    }
}
