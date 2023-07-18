using Content.Shared.Radio;
using Content.Shared.Random;
using Content.Shared.M_Emp;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.M_Emp
{
    /// <summary>
    /// A M_Emp generator.
    /// </summary>
    [NetworkedComponent, RegisterComponent]
    [Access(typeof(M_EmpSystem))]
    public sealed class M_EmpGeneratorComponent : SharedM_EmpGeneratorComponent
    {
        /// <summary>
        /// The entity attached to the generator
        /// </summary>
 //       [ViewVariables(VVAccess.ReadOnly)]
 //       [DataField("attachedEntity")]
 //       public EntityUid? AttachedEntity;

        /// <summary>
        /// Current state of this generator
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("generatorState")]
        public GeneratorState GeneratorState = GeneratorState.Inactive;

        /// <summary>
        /// How long it takes for the generator to pull in the debris
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("baseAttachingTime")]
        public TimeSpan BaseAttachingTime = TimeSpan.FromSeconds(30);

        /// <summary>
        /// How long it actually takes for the generator to pull in the debris
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("attachingTime")]
        public TimeSpan AttachingTime = TimeSpan.FromSeconds(30);

        /// <summary>
        /// How long the generator can hold the debris until it starts losing the lock
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("holdTime")]
        public TimeSpan HoldTime = TimeSpan.FromSeconds(240);

        /// <summary>
        /// How long the generator can hold the debris while losing the lock
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("detachingTime")]
        public TimeSpan DetachingTime = TimeSpan.FromSeconds(30);

        /// <summary>
        /// How long the generator has to cool down for after use
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("baseCooldownTime")]
        public TimeSpan BaseCooldownTime = TimeSpan.FromSeconds(60);

        /// <summary>
        /// How long the generator actually has to cool down for after use
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("cooldownTime")]
        public TimeSpan CooldownTime = TimeSpan.FromSeconds(60);

        [DataField("M_EmpChannel", customTypeSerializer: typeof(PrototypeIdSerializer<RadioChannelPrototype>))]
        public string M_EmpChannel = "Security";

        /// <summary>
        /// Current how much charge the generator currently has
        /// </summary>
        [DataField("chargeRemaining")]
        public int ChargeRemaining = 5;

        /// <summary>
        /// How much capacity the generator can hold
        /// </summary>
        [DataField("chargeCapacity")]
        public int ChargeCapacity = 5;

        /// <summary>
        /// Used as a guard to prevent spamming the appearance system
        /// </summary>
        [DataField("previousCharge")]
        public int PreviousCharge = 5;

        /// <summary>
        /// The chance that a random procgen asteroid will be
        /// generated rather than a static M_Emp prototype.
        /// </summary>
 //       [DataField("asteroidChance"), ViewVariables(VVAccess.ReadWrite)]
 //       public float AsteroidChance = 0.6f;

        /// <summary>
        /// A weighted random prototype corresponding to
        /// what asteroid entities will be generated.
        /// </summary>
//        [DataField("asteroidPool", customTypeSerializer: typeof(PrototypeIdSerializer<WeightedRandomPrototype>)), ViewVariables(VVAccess.ReadWrite)]
//        public string AsteroidPool = "RandomAsteroidPool";
    }

    [CopyByRef, DataRecord]
    public record struct GeneratorState(GeneratorStateType StateType, TimeSpan Until)
    {
        public static readonly GeneratorState Inactive = new (GeneratorStateType.Inactive, TimeSpan.Zero);
    };

    public sealed class M_EmpGeneratorActivatedEvent : EntityEventArgs
    {
        public EntityUid Generator;

        public M_EmpGeneratorActivatedEvent(EntityUid generator)
        {
            Generator = generator;
        }
    }
    public enum GeneratorStateType
    {
        Inactive,
        Attaching,
        Holding,
        Detaching,
        CoolingDown,
    }
}
