using Content.Server.Storage.Components;
using Content.Shared.Inventory;
using Content.Shared.Materials;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared.Storage.EntitySystems;

/// <summary>
/// <see cref="MagnetPickupComponent"/>
/// </summary>
public sealed class LatheMagnetPickupSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMaterialStorageSystem _storage = default!;

    private static readonly TimeSpan ScanDelay = TimeSpan.FromSeconds(1);

    private EntityQuery<PhysicsComponent> _physicsQuery;

    public override void Initialize()
    {
        base.Initialize();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        SubscribeLocalEvent<LatheMagnetPickupComponent, MapInitEvent>(OnMagnetMapInit);
        SubscribeLocalEvent<LatheMagnetPickupComponent, EntityUnpausedEvent>(OnMagnetUnpaused);
    }

    private void OnMagnetUnpaused(EntityUid uid, LatheMagnetPickupComponent component, ref EntityUnpausedEvent args)
    {
        component.NextScan += args.PausedTime;
    }

    private void OnMagnetMapInit(EntityUid uid, LatheMagnetPickupComponent component, MapInitEvent args)
    {
        component.NextScan = _timing.CurTime;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var currentTime = _timing.CurTime;
        var query = EntityQueryEnumerator<LatheMagnetPickupComponent, MaterialStorageComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var comp, out var storage, out var xform))
        {
            if (comp.NextScan < currentTime)
                continue;

            comp.NextScan += ScanDelay;

            var parentUid = xform.ParentUid;
            var moverCoords = _transform.GetMoverCoordinates(uid, xform);

            foreach (var near in _lookup.GetEntitiesInRange(uid, comp.Range, LookupFlags.Dynamic | LookupFlags.Sundries))
            {
                if (comp.Whitelist?.IsValid(near, EntityManager) == false)
                    continue;

                if (!_physicsQuery.TryGetComponent(near, out var physics) || physics.BodyStatus != BodyStatus.OnGround)
                    continue;

                if (near == parentUid)
                    continue;

                // TODO: Probably move this to storage somewhere when it gets cleaned up
                // TODO: This sucks but you need to fix a lot of stuff to make it better
                // the problem is that stack pickups delete the original entity, which is fine, but due to
                // game state handling we can't show a lerp animation for it.
                var nearXform = Transform(near);
                var nearMap = nearXform.MapPosition;

                if (!_storage.TryInsertMaterialEntity(uid, near, uid, storage))
                    continue;
            }
        }
    }
}
