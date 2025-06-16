using Content.Shared._NF.Atmos.Systems;
using Content.Shared._NF.Atmos.Visuals;
using Content.Shared.Atmos;
using Robust.Shared.GameStates;

namespace Content.Shared._NF.Atmos.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedGasDepositSystem))]
public sealed partial class GasDepositExtractorComponent : Component
{
    /// <summary>
    /// Whether or not the extractor is on and extracting gas.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled;

    /// <summary>
    /// The amount of gas to extract per second, in mol/s.
    /// </summary>
    [DataField]
    public float ExtractionRate;

    /// <summary>
    /// The maximum pressure output, in kPa.
    /// </summary>
    [DataField]
    public float MaxTargetPressure = Atmospherics.MaxOutputPressure;

    [DataField, AutoNetworkedField]
    public float TargetPressure = Atmospherics.OneAtmosphere;

    /// <summary>
    /// The output temperature, in K.
    /// </summary>
    [DataField]
    public float OutputTemperature = Atmospherics.T20C;

    /// <summary>
    /// The entity to be extracted from.
    /// </summary>
    [DataField]
    public EntityUid? DepositEntity;

    [DataField("port")]
    public string PortName { get; set; } = "port";

    // Storing the last known extraction state.
    [ViewVariables]
    public GasDepositExtractorState LastState { get; set; } = GasDepositExtractorState.Off;
}
