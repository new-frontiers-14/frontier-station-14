using Content.Server.Atmos.Piping.Binary.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping.Binary.Components; // Frontier

namespace Content.Server.Atmos.Piping.Binary.Components
{
    [RegisterComponent]
    public sealed partial class GasPressurePumpComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("enabled")]
        public bool Enabled { get; set; } = true;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("inlet")]
        public string InletName { get; set; } = "inlet";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("outlet")]
        public string OutletName { get; set; } = "outlet";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("targetPressure")]
        public float TargetPressure { get; set; } = Atmospherics.OneAtmosphere;

        /// <summary>
        ///     Max pressure of the target gas (NOT relative to source).
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("maxTargetPressure")]
        public float MaxTargetPressure = Atmospherics.MaxOutputPressure;

        /// <summary>
        /// Frontier - Start the pump with the map.
        /// </summary>
        [DataField]
        public bool StartOnMapInit { get; set; } = false;

        /// <summary>
        /// Frontier - UI key to open
        /// </summary>
        [DataField]
        public GasPressurePumpUiKey UiKey = GasPressurePumpUiKey.Key;

        /// <summary>
        /// Frontier - if true, the pump can have its direction changed (bidirectional pump)
        /// </summary>
        [DataField]
        public bool SettableDirection { get; private set; }

        /// <summary>
        /// Frontier - if true, the pump is currently pumping inwards
        /// </summary>
        [DataField]
        [Access(typeof(GasPressurePumpSystem))]
        public bool PumpingInwards { get; set; }
    }
}
