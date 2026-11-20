using Content.Shared.Damage;

namespace Content.Shared.Armor;

public abstract partial class SharedArmorSystem
{
    /// <summary>Copy the complete modifier set so upgrading one item cannot mutate its prototype or another item.</summary>
    public void SetDamageCoefficient(Entity<ArmorComponent> ent, string type, float coefficient)
    {
        ent.Comp.Modifiers = new DamageModifierSet
        {
            Coefficients = new(ent.Comp.Modifiers.Coefficients),
            FlatReduction = new(ent.Comp.Modifiers.FlatReduction),
            IgnoreArmorPierceFlags = ent.Comp.Modifiers.IgnoreArmorPierceFlags
        };
        ent.Comp.Modifiers.Coefficients[type] = coefficient;
        Dirty(ent);
    }
}
