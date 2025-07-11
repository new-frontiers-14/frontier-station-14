using Content.Shared.Materials;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Verbs;
using Robust.Shared.Utility;
using Content.Shared._NF.Storage.Components;

namespace Content.Shared._NF.Storage.EntitySystems;

/// <summary>
/// <see cref="NFMaterialStorageMagnetPickupComponent"/>
/// </summary>
public sealed class NFMaterialStorageMagnetPickupSystem : BaseMagnetPickupSystem
{
    [Dependency] private readonly SharedMaterialStorageSystem _storage = default!;

    private EntityQuery<MaterialComponent> _materialQuery;
    private EntityQuery<MaterialStorageComponent> _storageQuery;

    public override void Initialize()
    {
        base.Initialize();
        _materialQuery = GetEntityQuery<MaterialComponent>();
        _storageQuery = GetEntityQuery<MaterialStorageComponent>();
        SubscribeLocalEvent<NFMaterialStorageMagnetPickupComponent, MapInitEvent>(OnMagnetMapInit);
        SubscribeLocalEvent<NFMaterialStorageMagnetPickupComponent, EntityUnpausedEvent>(OnMagnetUnpaused);
        SubscribeLocalEvent<NFMaterialStorageMagnetPickupComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<NFMaterialStorageMagnetPickupComponent, GetVerbsEvent<AlternativeVerb>>(AddToggleMagnetVerb);
    }

    private void OnMagnetUnpaused(EntityUid uid, NFMaterialStorageMagnetPickupComponent component, ref EntityUnpausedEvent args)
    {
        HandleMagnetUnpaused(uid, component, ref args);
    }

    private void OnMagnetMapInit(EntityUid uid, NFMaterialStorageMagnetPickupComponent component, MapInitEvent args)
    {
        HandleMagnetMapInit(uid, component, args);
    }

    private void AddToggleMagnetVerb(EntityUid uid, NFMaterialStorageMagnetPickupComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        HandleAddToggleMagnetVerb(uid, component, args);
    }

    private void OnExamined(EntityUid uid, NFMaterialStorageMagnetPickupComponent component, ExaminedEvent args)
    {
        HandleExamined(uid, component, args);
    }

    public bool ToggleMagnet(EntityUid uid, NFMaterialStorageMagnetPickupComponent comp)
    {
        return ToggleMagnet<NFMaterialStorageMagnetPickupComponent>(uid, comp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<NFMaterialStorageMagnetPickupComponent, MaterialStorageComponent, TransformComponent>();
        var currentTime = Timing.CurTime;

        while (query.MoveNext(out var uid, out var comp, out var storage, out var xform))
        {
            if (comp.NextScan > currentTime)
                continue;

            if (!comp.MagnetEnabled)
            {
                comp.NextScan = currentTime + NFMagnetPickupComponent.SlowScanDelay;
                continue;
            }

            // Early termination: Skip if storage is at capacity
            if (storage.MaterialWhiteList != null && storage.MaterialWhiteList.Count > 0)
            {
                bool hasSpace = false;
                foreach (var material in storage.MaterialWhiteList)
                {
                    if (_storage.CanChangeMaterialAmount(uid, material, 1, storage))
                    {
                        hasSpace = true;
                        break;
                    }
                }

                if (!hasSpace)
                {
                    comp.NextScan = currentTime + NFMagnetPickupComponent.SlowScanDelay;
                    continue;
                }
            }

            var parentUid = xform.ParentUid;
            var entitiesProcessed = 0;
            var successfulPickups = 0;
            var foundMaterials = false;

            foreach (var near in Lookup.GetEntitiesInRange(uid, comp.Range, LookupFlags.Dynamic | LookupFlags.Sundries))
            {
                if (entitiesProcessed >= NFMagnetPickupComponent.MaxEntitiesPerScan)
                    break;

                entitiesProcessed++;

                if (!ShouldProcessEntity(near, parentUid))
                    continue;

                if (!_materialQuery.HasComponent(near))
                    continue;

                foundMaterials = true;

                if (_storage.TryInsertMaterialEntity(uid, near, uid, storage))
                {
                    successfulPickups++;

                    if (successfulPickups >= NFMagnetPickupComponent.MaxPickupsPerScan)
                        break;
                }
            }

            comp.NextScan = currentTime + CalculateNextScanDelay(successfulPickups, foundMaterials);
        }
    }
}
