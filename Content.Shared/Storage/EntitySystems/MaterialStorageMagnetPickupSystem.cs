using Content.Server.Storage.Components;
using Content.Shared.Materials;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;
using Content.Shared.Examine;   // Frontier
using Content.Shared.Hands.Components;  // Frontier
using Content.Shared.Verbs;     // Frontier
using Robust.Shared.Utility;    // Frontier

namespace Content.Shared.Storage.EntitySystems;

/// <summary>
/// <see cref="MaterialStorageMagnetPickupComponent"/>
/// </summary>
public sealed class MaterialStorageMagnetPickupSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedMaterialStorageSystem _storage = default!;

    private static readonly TimeSpan ScanDelay = TimeSpan.FromSeconds(1);

    private EntityQuery<PhysicsComponent> _physicsQuery;

    public override void Initialize()
    {
        base.Initialize();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        SubscribeLocalEvent<MaterialStorageMagnetPickupComponent, MapInitEvent>(OnMagnetMapInit);
        SubscribeLocalEvent<MaterialStorageMagnetPickupComponent, EntityUnpausedEvent>(OnMagnetUnpaused);
        SubscribeLocalEvent<MaterialStorageMagnetPickupComponent, ExaminedEvent>(OnExamined);  // Frontier
        SubscribeLocalEvent<MaterialStorageMagnetPickupComponent, GetVerbsEvent<AlternativeVerb>>(AddToggleMagnetVerb);    // Frontier
    }

    private void OnMagnetUnpaused(EntityUid uid, MaterialStorageMagnetPickupComponent component, ref EntityUnpausedEvent args)
    {
        component.NextScan += args.PausedTime;
    }

    private void OnMagnetMapInit(EntityUid uid, MaterialStorageMagnetPickupComponent component, MapInitEvent args)
    {
        component.NextScan = _timing.CurTime + TimeSpan.FromSeconds(1); // Need to add 1 sec to fix a weird time bug with it that make it never start the magnet
    }

    // Frontier, used to add the magnet toggle to the context menu
    private void AddToggleMagnetVerb(EntityUid uid, MaterialStorageMagnetPickupComponent component, GetVerbsEvent<AlternativeVerb> args)
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
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/Spare/poweronoff.svg.192dpi.png")),
            Text = Loc.GetString("magnet-pickup-component-toggle-verb"),
            Priority = 3
        };

        args.Verbs.Add(verb);
    }

    // Frontier, used to show the magnet state on examination
    private void OnExamined(EntityUid uid, MaterialStorageMagnetPickupComponent component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("magnet-pickup-component-on-examine-main",
                        ("stateText", Loc.GetString(component.MagnetEnabled
                        ? "magnet-pickup-component-magnet-on"
                        : "magnet-pickup-component-magnet-off"))));
    }

    // Frontier, used to toggle the magnet on the ore bag/box
    public bool ToggleMagnet(EntityUid uid, MaterialStorageMagnetPickupComponent comp)
    {
        var query = EntityQueryEnumerator<MaterialStorageMagnetPickupComponent>();
        comp.MagnetEnabled = !comp.MagnetEnabled;

        return comp.MagnetEnabled;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<MaterialStorageMagnetPickupComponent, MaterialStorageComponent, TransformComponent>();
        var currentTime = _timing.CurTime;

        while (query.MoveNext(out var uid, out var comp, out var storage, out var xform))
        {
            if (comp.NextScan < currentTime)
                continue;

            comp.NextScan += ScanDelay;

            // Frontier - magnet disabled
            if (!comp.MagnetEnabled)
                continue;

            var parentUid = xform.ParentUid;

            foreach (var near in _lookup.GetEntitiesInRange(uid, comp.Range, LookupFlags.Dynamic | LookupFlags.Sundries))
            {
                if (!_physicsQuery.TryGetComponent(near, out var physics) || physics.BodyStatus != BodyStatus.OnGround)
                    continue;

                if (near == parentUid)
                    continue;

                if (!_storage.TryInsertMaterialEntity(uid, near, uid, storage))
                    continue;
            }
        }
    }
}
