using Content.Shared.Damage;

namespace Content.Server.Bed.Components
{
    [RegisterComponent]
    public sealed partial class HealOnBuckleComponent : Component
    {
        /// <summary>
        /// Damage to apply to entities that are strapped to this entity.
        /// </summary>
        [DataField(required: true)]
        public DamageSpecifier Damage = default!;

        /// <summary>
        /// How frequently the damage should be applied, in seconds.
        /// </summary>
        [DataField(required: false)]
        public float HealTime = 1f;

        /// <summary>
        /// Damage multiplier that gets applied if the entity is sleeping.
        /// </summary>
        [DataField]
        public float SleepMultiplier = 3f;

        public TimeSpan NextHealTime = TimeSpan.Zero; //Next heal

        [DataField] public EntityUid? SleepAction;

        // Frontier: extra fields
        /// <summary>
        /// If true, this will only heal when powered
        /// </summary>
        [DataField]
        public bool WorksOnDead;

        /// <summary>
        /// If true, this will only heal when powered
        /// </summary>
        [DataField]
        public bool RequiresPower;

        /// <summary>
        /// If not null, this bed will heal damage only up to a maximum
        /// </summary>
        [DataField]
        public int? MaxDamage = null;
        // End Frontier: extra fields
    }
}
