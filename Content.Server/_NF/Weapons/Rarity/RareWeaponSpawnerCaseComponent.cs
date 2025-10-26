using Robust.Shared.Random;

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

    /// <summary>
    /// Modifier that can be applied to the gun's accuracy.
    /// </summary>
    [DataField]
    public StatModifier AccuracyModifier = new StatModifier(0.65f, 0.85f);

    /// <summary>
    /// Modifier that can be applied to the gun's projectile speed.
    /// </summary>
    [DataField]
    public StatModifier ProjectileSpeedModifier = new StatModifier(1.15f, 1.35f);

    /// <summary>
    /// Modifier that can be applied to the gun's fire rate.
    /// </summary>
    [DataField]
    public StatModifier FireRateModifier = new StatModifier(1.15f, 1.35f);
}

// Note: can't use MinMax here, because that only takes integers.
[DataDefinition, Serializable]
public partial struct StatModifier
{
    [DataField(required: true)]
    public float Min;

    [DataField(required: true)]
    public float Max;

    public StatModifier(float min, float max)
    {
        Min = min;
        Max = max;
    }

    public readonly float Next(IRobustRandom random)
    {
        return random.NextFloat(Min, Max);
    }
}
