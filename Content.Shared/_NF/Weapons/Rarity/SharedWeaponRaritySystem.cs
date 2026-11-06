using Content.Shared.Examine;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Wieldable;
using Robust.Shared.Utility;

namespace Content.Shared._NF.Weapons.Rarity;

public abstract class SharedWeaponRaritySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RareWeaponComponent, GunRefreshModifiersEvent>(OnGunRefreshModifiers,
            before: [typeof(SharedWieldableSystem)]); // Must apply before wielding bonus
        SubscribeLocalEvent<RareWeaponComponent, RefreshNameModifiersEvent>(OnRefreshNameModifiers);
        SubscribeLocalEvent<RareWeaponComponent, ExaminedEvent>(OnExamineWeapon);
    }

    private void OnGunRefreshModifiers(Entity<RareWeaponComponent> ent, ref GunRefreshModifiersEvent args)
    {
        var accuracyModifier = ent.Comp.AccuracyModifier;
        args.MinAngle *= accuracyModifier;
        args.MaxAngle *= accuracyModifier;
        args.AngleIncrease *= accuracyModifier;
        args.AngleDecay *= 1 / accuracyModifier;

        args.FireRate *= ent.Comp.FireRateModifier;

        args.ProjectileSpeed *= ent.Comp.ProjectileSpeedModifier;
    }

    private void OnRefreshNameModifiers(Entity<RareWeaponComponent> ent, ref RefreshNameModifiersEvent args)
    {
        foreach (var modifier in ent.Comp.NameModifiers)
        {
            args.AddModifier(modifier);
        }
    }

    private void OnExamineWeapon(Entity<RareWeaponComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.Rarity == WeaponRarity.Common)
            // No flavour for common weapons
            return;

        var message = new FormattedMessage();
        message.AddMarkupOrThrow(Loc.GetString(ent.Comp.Rarity switch
        {
            WeaponRarity.Uncommon => "weapon-description-rarity-uncommon",
            WeaponRarity.Rare => "weapon-description-rarity-rare",
            WeaponRarity.Epic => "weapon-description-rarity-epic",
            // Anything above epic is shown as 'legendary'. Admeme guns etc.
            // Common is excluded above.
            _ => "weapon-description-rarity-legendary",
        }));
        message.PushNewline();

        args.AddMessage(message);
    }
}
