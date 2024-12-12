using Content.Shared._NF.Atmos.Visuals;
using Content.Shared.Atmos;

namespace Content.Server._NF.Atmos.Components;

[RegisterComponent]
public sealed partial class GasDepositExtractorComponent : Component
{
    /// <summary>
    /// Whether or not the extractor is on and extracting gas.
    /// </summary>
    [DataField]
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
    public float MaxOutputPressure = Atmospherics.MaxOutputPressure;

    [DataField]
    public float TargetPressure = Atmospherics.OneAtmosphere;

    /// <summary>
    /// The output temperature, in K.
    /// </summary>
    [DataField]
    public float OutputTemperature = Atmospherics.T20C;

    /// <summary>
    /// The entity to be extracted from.
    /// </summary>
    /// <remarks>
    /// Should abstract into a general GasDepositComponent later.
    /// </remarks>
    [DataField]
    public Entity<RandomGasDepositComponent>? DepositEntity;

    [DataField("port")]
    public string PortName { get; set; } = "port";

    // Storing the last
    [ViewVariables]
    public GasDepositExtractorState LastState { get; set; } = GasDepositExtractorState.Off;
}
