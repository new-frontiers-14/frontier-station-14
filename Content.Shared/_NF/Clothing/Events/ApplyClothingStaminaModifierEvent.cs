[ByRefEvent]
/// <summary>
/// Raised when stamina damage should be modified by clothing
/// </summary>
public record struct ApplyClothingStaminaModifierEvent(EntityUid User, DamageContext Context, float StaminaDamage);