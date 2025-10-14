using Content.Server.Storage.Events;
using Content.Shared.Dataset;
using Content.Shared.NameModifier.EntitySystems;
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
    [Dependency] private readonly NameModifierSystem _namingSystem = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RareWeaponSpawnerCaseComponent, StorageFilledEvent>(OnCaseSpawn);
        SubscribeLocalEvent<RareWeaponComponent, RefreshNameModifiersEvent>(OnRefreshNameModifiers);
    }

    private void OnCaseSpawn(Entity<RareWeaponSpawnerCaseComponent> ent, ref StorageFilledEvent args)
    {
        if (!TryComp(ent, out StorageComponent? storage))
            return;

        foreach (var item in storage.StoredItems)
        {
            if (TryComp(item.Key, out GunComponent? gunComp))
            {
                ModifyGun(ent.Comp, item.Key, gunComp);
            }
        }
    }

    private void ModifyGun(RareWeaponSpawnerCaseComponent comp, EntityUid gun, GunComponent gunComp)
    {
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
                    ImproveAccuracy(comp.AccuracyModifier, gun, gunComp);
                    break;
                case 2:
                    ImproveFireRate(comp.FireRateModifier, gun, gunComp);
                    break;
                case 3:
                    ImproveProjectileSpeed(comp.ProjectileSpeedModifier, gun, gunComp);
                    break;
            }
            rarity--;
        }
    }

    private void ImproveAccuracy(StatModifier modifier, EntityUid gun, GunComponent? gunComp)
    {
        if (!Resolve(gun, ref gunComp))
            return;

        var buff = modifier.Next(_random);

        gunComp.MinAngle *= buff;
        gunComp.MinAngleModified *= buff;
        gunComp.MaxAngle *= buff;
        gunComp.MaxAngleModified *= buff;
        gunComp.AngleIncrease *= buff;
        gunComp.AngleIncreaseModified *= buff;
        gunComp.AngleDecay *= 1 / buff;
        gunComp.AngleDecayModified *= 1 / buff;
    }

    private void ImproveFireRate(StatModifier modifier, EntityUid gun, GunComponent? gunComp)
    {
        if (!Resolve(gun, ref gunComp))
            return;

        var buff = modifier.Next(_random);

        gunComp.FireRate *= buff;
        gunComp.FireRateModified *= buff;
    }

    private void ImproveProjectileSpeed(StatModifier modifier, EntityUid gun, GunComponent? gunComp)
    {
        if (!Resolve(gun, ref gunComp))
            return;

        var buff = modifier.Next(_random);

        gunComp.ProjectileSpeed *= buff;
        gunComp.ProjectileSpeedModified *= buff;
    }

    private void RenameGun(EntityUid gun, int rarity)
    {
        var datasetUncommon = _protoMan.Index<LocalizedDatasetPrototype>("NFNamesGunsUncommon");
        var datasetRare = _protoMan.Index<LocalizedDatasetPrototype>("NFNamesGunsRare");
        var datasetEpic = _protoMan.Index<LocalizedDatasetPrototype>("NFNamesGunsEpic");

        var meta = MetaData(gun);
        var rareComp = EnsureComp<RareWeaponComponent>(gun);

        if (rarity == 1)
        {
            rareComp.NameModifiers.Add(_random.Pick(datasetUncommon.Values));
        }
        else if (rarity == 2)
        {
            rareComp.NameModifiers.Add(_random.Pick(datasetUncommon.Values));
            rareComp.NameModifiers.Add(_random.Pick(datasetRare.Values));
        }
        else if (rarity >= 3)
        {
            // At this rarity, we rename the entire gun instead of adding a modifier
            var pick = _random.Pick(datasetEpic.Values);
            _metaSystem.SetEntityName(gun, Loc.GetString(pick), meta, false);
        }

        _namingSystem.RefreshNameModifiers(gun);
    }

    private void OnRefreshNameModifiers(Entity<RareWeaponComponent> ent, ref RefreshNameModifiersEvent args)
    {
        foreach (var modifier in ent.Comp.NameModifiers)
        {
            args.AddModifier(modifier);
        }
    }
}
