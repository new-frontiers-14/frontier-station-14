using Robust.Shared.GameStates;
using Content.Shared.Atmos.Piping.Binary.Components; // Frontier

namespace Content.Shared.Atmos.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GasPressurePumpComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    [DataField("inlet"), AutoNetworkedField] // Frontier: add AutoNetworkedField
    public string InletName = "inlet";

    [DataField("outlet"), AutoNetworkedField] // Frontier: add AutoNetworkedField
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
    [DataField, AutoNetworkedField]
    public bool PumpingInwards { get; set; }
}
