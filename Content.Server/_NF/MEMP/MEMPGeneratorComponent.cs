using Content.Shared._NF.MEMP;

namespace Content.Server._NF.MEMP
{
    [RegisterComponent]
    [Access(typeof(MEMPGeneratorSystem))]
    public sealed partial class MEMPGeneratorComponent : SharedMEMPGeneratorComponent
    {
        [DataField] public float LightRadiusMin { get; set; }
        [DataField] public float LightRadiusMax { get; set; }
    }
}
