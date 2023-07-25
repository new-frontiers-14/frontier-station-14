using Content.Shared.Radio;
using Content.Shared.Random;
using Content.Shared._NF.M_Emp;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._NF.M_Emp
{
    /// <summary>
    /// A M_Emp generator.
    /// </summary>
    [NetworkedComponent, RegisterComponent]
    [Access(typeof(M_EmpSystem))]
    public sealed class M_EmpGeneratorComponent : SharedM_EmpGeneratorComponent
    {
        /// <summary>
        /// Current state of this generator
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("generatorState")]
        public GeneratorState GeneratorState = GeneratorState.Inactive;

        /// <summary>
        /// How long it takes for the generator to EMP
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("baseActivatingTime")]
        public TimeSpan BaseActivatingTime = TimeSpan.FromSeconds(3.5);

        /// <summary>
        /// How long it actually takes for the generator to EMP
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("activatingTime")]
        public TimeSpan ActivatingTime = TimeSpan.FromSeconds(3.5);

        /// <summary>
        /// How long the generator EMP is working
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("engagedTime")]
        public TimeSpan EngagedTime = TimeSpan.FromSeconds(60);

        /// <summary>
        /// How long the generator Cooling Down
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("coolingDownTime")]
        public TimeSpan CoolingDownTime = TimeSpan.FromSeconds(60);

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
        Activating,
        Engaged,
        CoolingDown,
        Recharging,
    }
}
