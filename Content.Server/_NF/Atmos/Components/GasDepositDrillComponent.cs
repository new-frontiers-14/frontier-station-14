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
    public float MaxOutputPressure;

    /// <summary>
    /// The entity to be extracted from.
    /// </summary>
    [DataField]
    public EntityUid? DepositEntity;

    [DataField("port")]
    public string PortName { get; set; } = "port";
}
