using Content.Shared.Damage;

namespace Content.Server.Psionics
{
    [RegisterComponent]
    public sealed partial class AntiPsionicWeaponComponent : Component
    {

        [DataField("modifiers", required: true)]
        public DamageModifierSet Modifiers = default!;

        [DataField("psychicStaminaDamage")]
        public float PsychicStaminaDamage = 30f;

        [DataField("disableChance")]
        public float DisableChance = 0.3f;

        /// <summary>
        ///     Punish when used against a non-psychic.
        /// </summary
        [DataField("punish")]
        public bool Punish = true;
    }
}
