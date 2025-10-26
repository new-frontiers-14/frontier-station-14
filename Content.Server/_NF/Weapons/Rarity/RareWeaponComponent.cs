namespace Content.Server._NF.Weapons.Rarity;

/// <summary>
/// Added to a weapon to indicate that it's been modified in some way.
/// </summary>
[RegisterComponent]
[Access(typeof(WeaponRaritySystem))]
public sealed partial class RareWeaponComponent : Component
{
    [DataField]
    public List<string> NameModifiers = new();
}
