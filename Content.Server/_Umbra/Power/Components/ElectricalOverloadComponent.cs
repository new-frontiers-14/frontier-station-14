namespace Content.Server.Power.Components;

[RegisterComponent]
public sealed partial class ElectricalOverloadComponent : Component
{
    [ViewVariables]
    public DateTime EmpAt = DateTime.MaxValue;

    [ViewVariables]
    public DateTime NextBuzz = DateTime.MaxValue;

    /// <summary>
    /// Range of the EMP in tiles.
    /// </summary>
    [DataField]
    public float EmpRange = 1f;

    /// <summary>
    /// Power consumed from batteries by the EMP
    /// </summary>
    [DataField]
    public float EmpConsumption = 100000f;

    /// <summary>
    /// How long the EMP effects last for, in seconds
    /// </summary>
    [DataField]
    public float EmpDuration = 15f;
}
