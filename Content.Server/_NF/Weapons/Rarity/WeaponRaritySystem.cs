using Content.Server._NF.Weapons.Rarity;
using Content.Server.Storage.Events;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Random;

namespace Content.Server._NF.Weapons.Rarity;

public sealed partial class WeaponRaritySystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
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

        var rarity = comp.Rarity;
        if (comp.RandomRarity)
        {
            rarity = _random.Next(0, 4);
        }

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

        var buff = 1 - _random.NextFloat(0.1f, 0.25f);

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

        var buff = 1 + _random.NextFloat(0.1f, 0.25f);

        gunComp.FireRate *= buff;
        gunComp.FireRateModified *= buff;
    }

    private void ImproveProjectileSpeed(EntityUid gun, GunComponent? gunComp)
    {
        if (!Resolve(gun, ref gunComp))
            return;

        var buff = 1 + _random.NextFloat(0.1f, 0.25f);

        gunComp.ProjectileSpeed *= buff;
        gunComp.ProjectileSpeedModified *= buff;
    }
}
