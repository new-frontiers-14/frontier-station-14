using Content.Shared.Gravity;
using Content.Shared.Construction.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Gravity
{
    [RegisterComponent]
    [Access(typeof(GravityGeneratorSystem))]
    public sealed partial class GravityGeneratorComponent : SharedGravityGeneratorComponent
    {
        [DataField("lightRadiusMin")] public float LightRadiusMin { get; set; }
        [DataField("lightRadiusMax")] public float LightRadiusMax { get; set; }

<<<<<<< HEAD

        /// <summary>
        /// Is the power switch on?
        /// </summary>
        [DataField("switchedOn")]
        public bool SwitchedOn { get; set; } = true;

        /// <summary>
        /// Is the gravity generator intact?
        /// </summary>
        [DataField("intact")]
        public bool Intact { get; set; } = true;

        [DataField("maxCharge")]
        public float MaxCharge { get; set; } = 1;

        // 0 -> 1
        [ViewVariables(VVAccess.ReadWrite)] [DataField("charge")] public float Charge { get; set; } = 1;

        [DataField("machinePartMaxChargeMultiplier", customTypeSerializer: typeof(PrototypeIdSerializer<MachinePartPrototype>))]
        public string MachinePartMaxChargeMultiplier = "Capacitor";

=======
>>>>>>> a7e29f2878a63d62c9c23326e2b8f2dc64d40cc4
        /// <summary>
        /// Is the gravity generator currently "producing" gravity?
        /// </summary>
        [ViewVariables]
        public bool GravityActive { get; set; } = false;
    }
}
