using Content.Shared.Damage;
/// <summary>
/// Raised when a damage value should be modified by clothing
/// </summary>
[ByRefEvent]
public record struct ApplyClothingDamageModifierEvent(EntityUid User, DamageContext Context, DamageSpecifier Damage);