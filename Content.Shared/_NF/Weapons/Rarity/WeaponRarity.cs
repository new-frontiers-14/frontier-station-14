namespace Content.Shared._NF.Weapons.Rarity;

/// <summary>
/// This enum is used by the <see cref="RareWeaponComponent"/> to record the generated rarity
/// of a gun, by the RareWeaponSpawnerCaseComponent (server) for the highest rarity that
/// the case can generate, and the underlying integral value determines how many rounds
/// of improvements are applied to the weapon.
/// </summary>
public enum WeaponRarity : byte
{
    Common = 0,
    Uncommon = 1,
    Rare = 2,
    Epic = 3,
    Legendary = 4,
    /// <summary>
    /// Admeme-level gun, preposterously upgraded.
    /// </summary>
    Unique = 5,
}
