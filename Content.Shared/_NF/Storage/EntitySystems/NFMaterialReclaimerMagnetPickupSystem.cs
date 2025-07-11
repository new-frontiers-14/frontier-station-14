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
/// <see cref="NFMaterialReclaimerMagnetPickupComponent"/>
/// </summary>
public sealed class NFMaterialReclaimerMagnetPickupSystem : BaseMagnetPickupSystem
{
    [Dependency] private readonly SharedMaterialReclaimerSystem _storage = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NFMaterialReclaimerMagnetPickupComponent, MapInitEvent>(OnMagnetMapInit);
        SubscribeLocalEvent<NFMaterialReclaimerMagnetPickupComponent, EntityUnpausedEvent>(OnMagnetUnpaused);
        SubscribeLocalEvent<NFMaterialReclaimerMagnetPickupComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<NFMaterialReclaimerMagnetPickupComponent, GetVerbsEvent<AlternativeVerb>>(AddToggleMagnetVerb);
    }

    private void OnMagnetUnpaused(EntityUid uid, NFMaterialReclaimerMagnetPickupComponent component, ref EntityUnpausedEvent args)
    {
        HandleMagnetUnpaused(uid, component, ref args);
    }

    private void OnMagnetMapInit(EntityUid uid, NFMaterialReclaimerMagnetPickupComponent component, MapInitEvent args)
    {
        HandleMagnetMapInit(uid, component, args);
    }

    private void AddToggleMagnetVerb(EntityUid uid, NFMaterialReclaimerMagnetPickupComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        HandleAddToggleMagnetVerb(uid, component, args);
    }

    private void OnExamined(EntityUid uid, NFMaterialReclaimerMagnetPickupComponent component, ExaminedEvent args)
    {
        HandleExamined(uid, component, args);
    }

    public bool ToggleMagnet(EntityUid uid, NFMaterialReclaimerMagnetPickupComponent comp)
    {
        return ToggleMagnet<NFMaterialReclaimerMagnetPickupComponent>(uid, comp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<NFMaterialReclaimerMagnetPickupComponent, MaterialReclaimerComponent, TransformComponent>();
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

            var parentUid = xform.ParentUid;
            var entitiesProcessed = 0;
            var successfulPickups = 0;
            var foundTargets = false;

            foreach (var near in Lookup.GetEntitiesInRange(uid, comp.Range, LookupFlags.Dynamic | LookupFlags.Sundries))
            {
                if (entitiesProcessed >= NFMagnetPickupComponent.MaxEntitiesPerScan)
                    break;

                entitiesProcessed++;

                if (!ShouldProcessEntity(near, parentUid))
                    continue;

                foundTargets = true;

                if (_storage.TryStartProcessItem(uid, near))
                {
                    successfulPickups++;

                    if (successfulPickups >= NFMagnetPickupComponent.MaxPickupsPerScan)
                        break;
                }
            }

            comp.NextScan = currentTime + CalculateNextScanDelay(successfulPickups, foundTargets);
        }
    }
}
