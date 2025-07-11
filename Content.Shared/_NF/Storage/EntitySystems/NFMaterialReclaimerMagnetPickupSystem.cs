using Content.Shared.Materials;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Verbs;
using Robust.Shared.Utility;
using Content.Shared._NF.Storage.Components;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Storage.Components; // Added for StorageFillVisuals

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
        SubscribeLocalEvent<NFMaterialReclaimerMagnetPickupComponent, ItemToggledEvent>(OnItemToggled);
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

    private void OnItemToggled(EntityUid uid, NFMaterialReclaimerMagnetPickupComponent component, ref ItemToggledEvent args)
    {
        HandleItemToggled(uid, component, ref args);
        Dirty(uid, component);
    }

    public bool ToggleMagnet(EntityUid uid, NFMaterialReclaimerMagnetPickupComponent comp)
    {
        var result = ToggleMagnet<NFMaterialReclaimerMagnetPickupComponent>(uid, comp);
        Dirty(uid, comp);
        return result;
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

            // Check auto-disable before processing
            if (CheckAutoDisable(uid, comp))
            {
                Dirty(uid, comp); // Mark as dirty if auto-disabled
                comp.NextScan = currentTime + NFMagnetPickupComponent.SlowScanDelay;
                continue;
            }

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

            // Handle successful pickup tracking for auto-disable
            HandleSuccessfulPickup(uid, comp, successfulPickups);

            comp.NextScan = currentTime + CalculateNextScanDelay(successfulPickups, foundTargets);
        }
    }
}
