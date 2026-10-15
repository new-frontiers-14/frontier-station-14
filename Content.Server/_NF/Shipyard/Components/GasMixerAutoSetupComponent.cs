using Content.Shared.Atmos;

namespace Content.Server._NF.Shipyard.Components;

[RegisterComponent]
public sealed partial class GasMixerAutoSetupComponent : Component
{
    /// <summary>
    /// The gas expected to be provided on the straight-through inlet
    /// </summary>
    [DataField(required: true)]
    public Gas InletOneGas;

    /// <summary>
    /// The gas expected to be provided on the side inlet
    /// </summary>
    [DataField(required: true)]
    public Gas InletTwoGas;
}
