using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GasPressurePumpComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    [DataField("inlet")]
    public string InletName = "inlet";

    [DataField("outlet")]
    public string OutletName = "outlet";

    [DataField, AutoNetworkedField]
    public float TargetPressure = Atmospherics.OneAtmosphere;

    /// <summary>
    ///     Max pressure of the target gas (NOT relative to source).
    /// </summary>
    [DataField]
    public float MaxTargetPressure = Atmospherics.MaxOutputPressure;

    /// <summary>
    /// Frontier - Start the pump with the map.
    /// </summary>
    [DataField]
    public bool StartOnMapInit { get; set; }
}
