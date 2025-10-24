using Content.Server.Storage.Events;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared._NF.Weapons.Rarity;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Storage;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._NF.Weapons.Rarity;

/// <summary>
/// A stand-alone system intended to modularly sit atop the existing gun and weapon systems to create dynamic weapon rarities.
/// </summary>
public sealed partial class WeaponRaritySystem : SharedWeaponRaritySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly GunSystem _gunSystem = default!;
    [Dependency] private readonly NameModifierSystem _namingSystem = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RareWeaponSpawnerCaseComponent, StorageFilledEvent>(OnCaseSpawn);
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
        var rarity = comp.Rarity;
        if (comp.RandomRarity)
        {
            rarity = (WeaponRarity)_random.Next(0, (int)rarity + 1);
        }

        if (rarity == 0)
        {
            // Don't add unnecessary components for common rarity
            return;
        }

        var rareComp = EnsureComp<RareWeaponComponent>(gun);
        rareComp.Rarity = rarity;
        ImproveGun(comp, gun, rareComp);
        RenameGun(comp, gun, rareComp);
        Dirty(gun, rareComp);
    }

    private void ImproveGun(RareWeaponSpawnerCaseComponent comp, EntityUid gun, RareWeaponComponent rareComp)
    {
        var rarity = rareComp.Rarity;
        while (rarity > 0)
        {
            switch (_random.Next(0, 3))
            {
                case 0:
                    rareComp.AccuracyModifier *= comp.AccuracyModifier.Next(_random, rarity);
                    break;
                case 1:
                    rareComp.FireRateModifier *= comp.FireRateModifier.Next(_random, rarity);
                    break;
                case 2:
                    rareComp.ProjectileSpeedModifier *= comp.ProjectileSpeedModifier.Next(_random, rarity);
                    break;
            }
            rarity--;
        }

        _gunSystem.RefreshModifiers(gun);
    }

    private void RenameGun(RareWeaponSpawnerCaseComponent spawnerComp, EntityUid gun, RareWeaponComponent rareComp)
    {
        var datasetUncommon = _protoMan.Index(spawnerComp.UncommonNameSet);
        var datasetRare = _protoMan.Index(spawnerComp.RareNameSet);
        var datasetEpic = _protoMan.Index(spawnerComp.EpicNameSet);

        var meta = MetaData(gun);

        switch (rareComp.Rarity)
        {
            case WeaponRarity.Common:
                break;
            case WeaponRarity.Uncommon:
                rareComp.NameModifiers.Add(_random.Pick(datasetUncommon.Values));
                break;
            case WeaponRarity.Rare:
                rareComp.NameModifiers.Add(_random.Pick(datasetUncommon.Values));
                rareComp.NameModifiers.Add(_random.Pick(datasetRare.Values));
                break;
            case WeaponRarity.Epic:
            case WeaponRarity.Legendary:
            case WeaponRarity.Unique:
            default: // higher rarities from admemes etc.
                // At these rarities, we rename the entire gun instead of adding a modifier
                var pick = _random.Pick(datasetEpic.Values);
                _metaSystem.SetEntityName(gun, Loc.GetString(pick), meta, false);
                break;
        }

        _namingSystem.RefreshNameModifiers(gun);
    }
}
