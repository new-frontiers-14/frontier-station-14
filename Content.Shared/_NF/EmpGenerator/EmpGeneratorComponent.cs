using Robust.Shared.GameStates;

namespace Content.Shared._NF.EmpGenerator;

[RegisterComponent, NetworkedComponent]
public sealed partial class EmpGeneratorComponent : Component
{
    /// <summary>
    /// The range of the EMP blast to spawn.
    /// </summary>
    [DataField]
    public float Range = 100.0f;

    /// <summary>
    /// How much energy will be consumed per battery in range
    /// </summary>
    [DataField]
    public float EnergyConsumption = 1000000;

    /// <summary>
    /// How long it disables targets in seconds
    /// </summary>
    [DataField]
    public float DisableDuration = 60f;

    [DataField(serverOnly: true)]
    public float LightRadiusMin { get; set; }

    [DataField(serverOnly: true)]
    public float LightRadiusMax { get; set; }
}
