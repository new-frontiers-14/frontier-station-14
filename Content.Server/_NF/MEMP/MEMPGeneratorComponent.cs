using Content.Shared._NF.MEMP;

namespace Content.Server._NF.MEMP
{
    [RegisterComponent]
    [Access(typeof(MEMPGeneratorSystem))]
    public sealed partial class MEMPGeneratorComponent : SharedMEMPGeneratorComponent
    {
        [DataField] public float LightRadiusMin { get; set; }
        [DataField] public float LightRadiusMax { get; set; }

        /// <summary>
        /// Is the mobile emp currently running?
        /// </summary>
        [ViewVariables]
        public bool MEMPActive { get; set; } = false;

        /// <summary>
        /// Is the mobile emp action locked.
        /// </summary>
        [ViewVariables]
        public bool MEMPActionLocked { get; set; } = false;
    }
}
