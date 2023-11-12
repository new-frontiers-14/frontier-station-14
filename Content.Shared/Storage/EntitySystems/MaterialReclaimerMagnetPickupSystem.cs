using Content.Server.Storage.Components;
using Content.Shared.Materials;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Storage.EntitySystems;

/// <summary>
/// <see cref="MaterialReclaimerMagnetPickupComponent"/>
/// </summary>
public sealed class MaterialReclaimerMagnetPickupSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedMaterialReclaimerSystem _storage = default!;

    private static readonly TimeSpan ScanDelay;

    private EntityQuery<PhysicsComponent> _physicsQuery;

    public override void Initialize()
    {
        base.Initialize();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        SubscribeLocalEvent<MaterialReclaimerMagnetPickupComponent, MapInitEvent>(OnMagnetMapInit);
        SubscribeLocalEvent<MaterialReclaimerMagnetPickupComponent, EntityUnpausedEvent>(OnMagnetUnpaused);
    }

    private void OnMagnetUnpaused(EntityUid uid, MaterialReclaimerMagnetPickupComponent component, ref EntityUnpausedEvent args)
    {
        component.NextScan += args.PausedTime;
    }

    private void OnMagnetMapInit(EntityUid uid, MaterialReclaimerMagnetPickupComponent component, MapInitEvent args)
    {
        component.NextScan = _timing.CurTime;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<MaterialReclaimerMagnetPickupComponent, MaterialReclaimerComponent, TransformComponent>();
        var currentTime = _timing.CurTime;

        while (query.MoveNext(out var uid, out var comp, out var storage, out var xform))
        {
            if (comp.NextScan < currentTime)
                continue;

            comp.NextScan += ScanDelay;

            var parentUid = xform.ParentUid;

            foreach (var near in _lookup.GetEntitiesInRange(uid, comp.Range, LookupFlags.Dynamic | LookupFlags.Sundries))
            {
                if (!_physicsQuery.TryGetComponent(near, out var physics) || physics.BodyStatus != BodyStatus.OnGround)
                    continue;

                if (near == parentUid)
                    continue;

                if (!_storage.TryStartProcessItem(uid, near))
                    continue;
            }
        }
    }
}
