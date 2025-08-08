using Content.Server.Storage.Events;
using Content.Shared.Dataset;
using Content.Shared.Storage;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._NF.Weapons.Rarity;

/// <summary>
/// A stand-alone system intended to modularly sit atop the existing gun and weapon systems to create dynamic weapon rarities.
/// </summary>
public sealed partial class WeaponRaritySystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RareWeaponSpawnerCaseComponent, StorageFilledEvent>(OnCaseSpawn);
    }

    private void OnCaseSpawn(Entity<RareWeaponSpawnerCaseComponent> ent, ref StorageFilledEvent args)
    {
        var comp = ent.Comp;
        if (!TryComp<StorageComponent>(ent, out StorageComponent? storage))
            return;

        var contents = storage.StoredItems;
        GunComponent? gunComp = null;
        EntityUid gun = default;
        foreach (var item in contents)
        {
            if (TryComp(item.Key, out gunComp))
            {
                gun = item.Key;
                break;
            }
        }

        if (gunComp == null || !gun.IsValid() )
            return;

        //Basic functionality of 3 rarity levels
        var rarity = comp.Rarity;
        if (comp.RandomRarity)
        {
            rarity = _random.Next(0, 4);
        }

        RenameGun(gun, rarity);

        while (rarity > 0)
        {
            var chance = _random.Next(1, 4);
            switch (chance)
            {
                case 1:
                    ImproveAccuracy(gun, gunComp);
                    break;
                case 2:
                    ImproveFireRate(gun, gunComp);
                    break;
                case 3:
                    ImproveProjectileSpeed(gun, gunComp);
                    break;
            }
            rarity--;
        }
    }

    private void ImproveAccuracy(EntityUid gun, GunComponent? gunComp)
    {
        if (!Resolve(gun, ref gunComp))
            return;

        var buff = 1 - _random.NextFloat(0.15f, 0.35f);

        gunComp.MinAngle *= buff;
        gunComp.MinAngleModified *= buff;
        gunComp.MaxAngle *= buff;
        gunComp.MaxAngleModified *= buff;
        gunComp.AngleIncrease *= buff;
        gunComp.AngleIncreaseModified *= buff;
        gunComp.AngleDecay *= 1 / buff;
        gunComp.AngleDecayModified *= 1 / buff;
    }

    private void ImproveFireRate(EntityUid gun, GunComponent? gunComp)
    {
        if (!Resolve(gun, ref gunComp))
            return;

        var buff = 1 + _random.NextFloat(0.15f, 0.35f);

        gunComp.FireRate *= buff;
        gunComp.FireRateModified *= buff;
    }

    private void ImproveProjectileSpeed(EntityUid gun, GunComponent? gunComp)
    {
        if (!Resolve(gun, ref gunComp))
            return;

        var buff = 1 + _random.NextFloat(0.15f, 0.35f);

        gunComp.ProjectileSpeed *= buff;
        gunComp.ProjectileSpeedModified *= buff;
    }

    private void RenameGun(EntityUid gun, int rarity)
    {
        var datasetUncommon = _protoMan.Index<LocalizedDatasetPrototype>("NFNamesGunsUncommon");
        var datasetRare = _protoMan.Index<LocalizedDatasetPrototype>("NFNamesGunsRare");
        var datasetEpic = _protoMan.Index<LocalizedDatasetPrototype>("NFNamesGunsEpic");

        var meta = MetaData(gun);

        if (rarity == 1)
        {
            var pick = _random.Pick(datasetUncommon.Values);
            var newName = Loc.GetString(pick) + " " + meta.EntityName;
            _metaSystem.SetEntityName(gun, newName, meta, false);
        }
        else if (rarity == 2)
        {
            var pick1 = _random.Pick(datasetUncommon.Values);
            var pick2 = _random.Pick(datasetRare.Values);
            var newName = Loc.GetString(pick2) + " " + Loc.GetString(pick1)+ " " + meta.EntityName;
            _metaSystem.SetEntityName(gun, newName, meta, false);
        }
        else if (rarity >= 3)
        {
            var pick = _random.Pick(datasetEpic.Values);
            _metaSystem.SetEntityName(gun, Loc.GetString(pick), meta, false);
        }
    }
}
