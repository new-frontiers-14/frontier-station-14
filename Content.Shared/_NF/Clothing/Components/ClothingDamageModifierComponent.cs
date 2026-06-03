using Content.Shared.Damage;

namespace Content.Shared._NF.Clothing.Components;

[RegisterComponent]
public sealed partial class ClothingDamageModifierComponent : Component
{
    /// <summary>
    /// Set of flags to describe which damage contexts should get the bonuses
    /// </summary>
    [DataField]
    public HashSet<DamageContext> Affects = new();

    /// <summary>
    /// A DamageSpecifier describing what types of damage and how much of each type will be added to the damage.
    /// </summary>
    [DataField]
    public DamageSpecifier? BonusDamage;

    /// <summary>
    /// A modifier set describing what modifiers should apply to each damage type.
    /// </summary>
    [DataField]
    public DamageModifierSet? DamageModifierSet;

    /// <summary>
    /// A flat bonus added to stamina damage dealt in relevant contexts.
    /// </summary>
    [DataField]
    public float? StaminaFlatBonus;

    /// <summary>
    /// A multiplicative modifier applied to stamina damage in relevant contexts.
    /// </summary>
    [DataField]
    public float? StaminaMultiplier;

    /// <summary>
    /// Helper function to check whether the damage should be applied in a given context.
    /// </summary>
    public bool AppliesTo(DamageContext context) => Affects.Contains(context);

    /// <summary>
    /// Helper function to retrieve flat bonus damages in a given context.
    /// </summary>
    public DamageSpecifier? GetFlatBonus(DamageContext context) => AppliesTo(context) ? BonusDamage : null;

    /// <summary>
    /// Helper function to retrieve damage modifiers in a given context.
    /// </summary>
    public DamageModifierSet? GetModifierSet(DamageContext context) => AppliesTo(context) ? DamageModifierSet : null;
}