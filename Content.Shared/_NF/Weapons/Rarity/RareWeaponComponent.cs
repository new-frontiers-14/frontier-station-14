using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.GameStates;

namespace Content.Shared._NF.Weapons.Rarity;

/// <summary>
/// Added to a weapon to indicate that it's been modified in some way.
/// The modifiers are applied to the gun through the <see cref="GunRefreshModifiersEvent"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedWeaponRaritySystem))]
public sealed partial class RareWeaponComponent : Component
{
    [DataField, AutoNetworkedField]
    public WeaponRarity Rarity = WeaponRarity.Common;

    [DataField, AutoNetworkedField]
    public List<string> NameModifiers = new();

    [DataField, AutoNetworkedField]
    public float AccuracyModifier = 1.0f;

    [DataField, AutoNetworkedField]
    public float ProjectileSpeedModifier = 1.0f;

    [DataField, AutoNetworkedField]
    public float FireRateModifier = 1.0f;
}
