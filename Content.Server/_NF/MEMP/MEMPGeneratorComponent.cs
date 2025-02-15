using Content.Shared._NF.MEMP;
using Content.Shared.Construction.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._NF.MEMP
{
    [RegisterComponent]
    [Access(typeof(MEMPGeneratorSystem))]
    public sealed partial class MEMPGeneratorComponent : SharedMEMPGeneratorComponent
    {
        [DataField("lightRadiusMin")] public float LightRadiusMin { get; set; }
        [DataField("lightRadiusMax")] public float LightRadiusMax { get; set; }

        /// <summary>
        /// Is the gravity generator currently "producing" gravity?
        /// </summary>
        [ViewVariables]
        public bool MEMPActive { get; set; } = false;
    }
}
