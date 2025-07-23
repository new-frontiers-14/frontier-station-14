namespace Content.Server._NF.Weapons.Rarity;

/// <summary>
/// This component is added to weapon cases that transform the containing spawned weapon into a weapon with rare markers.
/// </summary>
[RegisterComponent]
public sealed partial class RareWeaponSpawnerCaseComponent : Component
{
    [DataField]
    public int Rarity = 1;

    [DataField]
    public bool RandomRarity = false;
}
