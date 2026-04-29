using Content.Shared._NF.Weapons.Rarity;
using Content.Shared.Dataset;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._NF.Weapons.Rarity;

/// <summary>
/// This component is added to weapon cases that transform the containing spawned weapon into a weapon with rare markers.
/// </summary>
[RegisterComponent]
public sealed partial class RareWeaponSpawnerCaseComponent : Component
{
    /// <summary>
    /// The rarity imbued by the weapon case. If <c>RandomRarity</c> is true, then this is
    /// the maximum rarity (inclusive) that can be applied.
    /// </summary>
    [DataField]
    public WeaponRarity Rarity = WeaponRarity.Uncommon;

    [DataField]
    public bool RandomRarity = false;

    /// <summary>
    /// Modifier that can be applied to the gun's accuracy.
    /// </summary>
    [DataField]
    public StatModifier AccuracyModifier = new(-0.35f, -0.15f); // lower is better

    /// <summary>
    /// Modifier that can be applied to the gun's projectile speed.
    /// </summary>
    [DataField]
    public StatModifier ProjectileSpeedModifier = new(0.15f, 0.35f, 1.075f); // less extreme scaling to prevent tunneling issues

    /// <summary>
    /// Modifier that can be applied to the gun's fire rate.
    /// </summary>
    [DataField]
    public StatModifier FireRateModifier = new(0.15f, 0.35f);

    /// <summary>
    /// Name modifier dataset for 'Uncommon' rarity.
    /// </summary>
    [DataField]
    public ProtoId<LocalizedDatasetPrototype> UncommonNameSet = "NFNamesGunsUncommon";

    /// <summary>
    /// Name modifier dataset for 'Rare' rarity.
    /// </summary>
    [DataField]
    public ProtoId<LocalizedDatasetPrototype> RareNameSet = "NFNamesGunsRare";

    /// <summary>
    /// Name replacement dataset for 'Epic' rarity.
    /// </summary>
    [DataField]
    public ProtoId<LocalizedDatasetPrototype> EpicNameSet = "NFNamesGunsEpic";
}

/// <summary>
/// A modifier applied to one of a weapon's stats. The modifier values are offsets from 1.
/// If Min = 0.2 and Max = 0.4, then the final modifier returned by <c>Next</c> will be
/// in the range 1.2 to 1.4 (exclusive) when rarity = 1. Negative values subtract from 1,
/// such that e.g. -0.4 to -0.2 results in a final modifier between 0.6 and 0.8.
/// This simplifies scaling by rarity.
/// </summary>
[DataDefinition, Serializable]
public partial struct StatModifier
{
    [DataField(required: true)]
    public float Min;

    [DataField(required: true)]
    public float Max;

    /// <summary>
    /// The scaling factor applied to the modifier based on the weapon's rarity.
    /// The higher the scaling factor, the more "oomph" you get for higher rarities.
    /// </summary>
    [DataField]
    public float RarityScaling = 1.125f;

    public StatModifier(float min, float max)
    {
        Min = min;
        Max = max;
    }

    public StatModifier(float min, float max, float rarityScaling)
    {
        Min = min;
        Max = max;
        RarityScaling = rarityScaling;
    }

    public readonly float Next(IRobustRandom random, WeaponRarity rarity)
    {
        // RarityScaling ** (-1) is bad news.
        if (rarity == 0)
        {
            DebugTools.Assert(false, "Attempted to compute stat modifier for rarity 0");
            return 1;
        }

        var modifier = random.NextFloat(Min, Max);
        // Exaggerate the effect based on the rarity level.
        // At lowest rarity, RarityScaling ** (1 - 1) = 1; anything above is an increase.
        modifier *= MathF.Pow(RarityScaling, (float)rarity - 1f);
        return modifier + 1;
    }
}
