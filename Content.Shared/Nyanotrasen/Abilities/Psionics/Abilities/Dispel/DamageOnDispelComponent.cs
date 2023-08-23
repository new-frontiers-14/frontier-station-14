using Content.Shared.Damage;

namespace Content.Shared.Abilities.Psionics
{
    /// <summary>
    /// Takes damage when dispelled.
    /// </summary>
    [RegisterComponent]
    public sealed class DamageOnDispelComponent : Component
    {
        [DataField("damage", required: true)]
        public DamageSpecifier  Damage = default!;

        [DataField("variance")]
        public float Variance = 0.5f;
    }
}
