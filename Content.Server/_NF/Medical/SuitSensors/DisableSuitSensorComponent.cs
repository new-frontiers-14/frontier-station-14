
namespace Content.Server._NF.Medical.SuitSensors;

// A component to disable suit sensors, regardless of settings, for a particular entity (e.g. medical corpses)
[RegisterComponent]
public sealed partial class DisableSuitSensorComponent : Component
{
    /// <summary>
    /// If true, runs only ones as the entity starting gear to disable any active cloth sensors.
    /// </summary>
    [DataField]
    public bool StartingGear = true;

    /// <summary>
    /// If true, runs every time.
    /// </summary>
    [DataField]
    public bool Always;
}
