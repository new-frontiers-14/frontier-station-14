using Content.Shared.Storage.Components; // Frontier: Server<Shared
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory;
using Content.Shared.Item.ItemToggle; // DeltaV
using Content.Shared.Verbs; // Frontier
using Content.Shared.Whitelist;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Storage.EntitySystems;

/// <summary>
/// <see cref="MagnetPickupComponent"/>
/// </summary>
public sealed class MagnetPickupSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!; // DeltaV
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;


    private static readonly TimeSpan ScanDelay = TimeSpan.FromSeconds(1);

    private EntityQuery<PhysicsComponent> _physicsQuery;

    public override void Initialize()
    {
        base.Initialize();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        SubscribeLocalEvent<MagnetPickupComponent, MapInitEvent>(OnMagnetMapInit);
        SubscribeLocalEvent<MagnetPickupComponent, ExaminedEvent>(OnExamined); // Frontier
        SubscribeLocalEvent<MagnetPickupComponent, GetVerbsEvent<AlternativeVerb>>(AddToggleMagnetVerb); // Frontier
    }

    private void OnMagnetMapInit(EntityUid uid, MagnetPickupComponent component, MapInitEvent args)
    {
        component.NextScan = _timing.CurTime;
    }


    // Frontier: togglable magnets
    private void AddToggleMagnetVerb(EntityUid uid, MagnetPickupComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        // Magnet run by other means (e.g. toggles)
        if (!component.MagnetCanBeEnabled)
            return;

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
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/Spare/poweronoff.svg.192dpi.png")),
            Text = Loc.GetString("magnet-pickup-component-toggle-verb"),
            Priority = component.MagnetTogglePriority // Frontier: 3 < component.MagnetTogglePriority
        };

        args.Verbs.Add(verb);
    }

    // Show the magnet state on examination
    private void OnExamined(EntityUid uid, MagnetPickupComponent component, ExaminedEvent args)
    {
        // Magnet run by other means (e.g. toggles)
        if (!component.MagnetCanBeEnabled)
            return;

        args.PushMarkup(Loc.GetString("magnet-pickup-component-on-examine-main",
                        ("stateText", Loc.GetString(component.MagnetEnabled
                        ? "magnet-pickup-component-magnet-on"
                        : "magnet-pickup-component-magnet-off"))));
    }

    //Toggles the magnet on the ore bag/box
    public void ToggleMagnet(EntityUid uid, MagnetPickupComponent comp)
    {
        // Magnet run by other means (e.g. toggles)
        if (!comp.MagnetCanBeEnabled)
            return;

        comp.MagnetEnabled = !comp.MagnetEnabled;
        Dirty(uid, comp);
    }
    // End Frontier: togglable magnets

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<MagnetPickupComponent, StorageComponent, TransformComponent, MetaDataComponent>();
        var currentTime = _timing.CurTime;

        while (query.MoveNext(out var uid, out var comp, out var storage, out var xform, out var meta))
        {
            if (comp.NextScan > currentTime)
                continue;

            comp.NextScan = currentTime + ScanDelay; // Frontier: no need to rerun if built late in-round

            // Frontier: combine DeltaV/White Dream's magnet toggle with old system
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
            // End Frontier

            // Begin DeltaV Removals: Allow ore bags to work inhand
            //if (!_inventory.TryGetContainingSlot((uid, xform, meta), out var slotDef))
            //    continue;

            //if ((slotDef.SlotFlags & comp.SlotFlags) == 0x0)
            //    continue;
            // End DeltaV Removals

            // No space
            if (!_storage.HasSpace((uid, storage)))
                continue;

            var parentUid = xform.ParentUid;
            var playedSound = false;
            var finalCoords = xform.Coordinates;
            var moverCoords = _transform.GetMoverCoordinates(uid, xform);

            foreach (var near in _lookup.GetEntitiesInRange(uid, comp.Range, LookupFlags.Dynamic | LookupFlags.Sundries))
            {
                if (_whitelistSystem.IsWhitelistFail(storage.Whitelist, near))
                    continue;

                // FRONTIER - START
                // Makes sure that after the last insertion, there is still bag space.
                // Using a cheap 'how many slots left' vs 'how many we need' check, and additional stack check.
                // Note: Unfortunately, this is still 'expensive' as it checks the entire bag, however its better than
                // to rotate an item n^n times of every item in the bag to find the best fit, for every xy coordinate it has.
                if (!_storage.HasSlotSpaceFor(uid, near))
                    continue;
                // FRONTIER - END

                if (!_physicsQuery.TryGetComponent(near, out var physics) || physics.BodyStatus != BodyStatus.OnGround)
                    continue;

                if (near == parentUid)
                    continue;

                // TODO: Probably move this to storage somewhere when it gets cleaned up
                // TODO: This sucks but you need to fix a lot of stuff to make it better
                // the problem is that stack pickups delete the original entity, which is fine, but due to
                // game state handling we can't show a lerp animation for it.
                var nearXform = Transform(near);
                var nearMap = _transform.GetMapCoordinates(near, xform: nearXform);
                var nearCoords = EntityCoordinates.FromMap(moverCoords.EntityId, nearMap, _transform, EntityManager);

                if (!_storage.Insert(uid, near, out var stacked, storageComp: storage, playSound: !playedSound))
                    continue;

                // Play pickup animation for either the stack entity or the original entity.
                if (stacked != null)
                    _storage.PlayPickupAnimation(stacked.Value, nearCoords, finalCoords, nearXform.LocalRotation);
                else
                    _storage.PlayPickupAnimation(near, nearCoords, finalCoords, nearXform.LocalRotation);

                playedSound = true;
            }
        }
    }
}
