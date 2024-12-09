namespace Content.Server.Emp;

/// <summary>
/// Generates an EMP description for an entity that won't otherwise get one.
/// </summary>
[RegisterComponent]
[Access(typeof(EmpSystem))]
public sealed partial class EmpDescriptionComponent : Component
{
    /// <summary>
    /// The range of the EMP blast, in meters
    /// </summary>
    [DataField]
    public float Range = 1.0f;

    /// <summary>
    /// How much energy will be consumed per battery in range
    /// </summary>
    [DataField]
    public float EnergyConsumption;

    /// <summary>
    /// How long it disables targets in seconds
    /// </summary>
    [DataField]
    public float DisableDuration = 10f;
}
