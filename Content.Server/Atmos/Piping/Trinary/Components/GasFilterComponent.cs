using Content.Shared.Atmos;

namespace Content.Server.Atmos.Piping.Trinary.Components
{
    [RegisterComponent]
    public sealed partial class GasFilterComponent : Component
    {
        [DataField]
        public bool Enabled = true;

        [DataField("inlet")]
        public string InletName = "inlet";

        [DataField("filter")]
        public string FilterName = "filter";

        [DataField("outlet")]
        public string OutletName = "outlet";

        [DataField]
        public float TransferRate = Atmospherics.MaxTransferRate;

        [DataField]
        public float MaxTransferRate = Atmospherics.MaxTransferRate;

<<<<<<< HEAD
        [DataField("maxTransferRate")]
        public float MaxTransferRate { get; set; } = Atmospherics.MaxTransferRate;

        [DataField, ViewVariables(VVAccess.ReadWrite)] // Frontier - Added DataField
        public Gas? FilteredGas { get; set; }

        /// <summary>
        /// Frontier - Start the filter with the map.
        /// </summary>
        [DataField]
        public bool StartOnMapInit { get; set; } = false;
=======
        [DataField]
        public Gas? FilteredGas;
>>>>>>> a7e29f2878a63d62c9c23326e2b8f2dc64d40cc4
    }
}
