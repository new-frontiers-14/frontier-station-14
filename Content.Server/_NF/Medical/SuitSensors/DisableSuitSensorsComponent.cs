namespace Content.Server._NF.Medical.SuitSensors;

// A component to disable suit sensors, regardless of settings, for a particular entity (e.g. medical corpses)
[RegisterComponent]
public sealed partial class DisableSuitSensorsComponent : Component
{
    /// <summary>
    /// If true, runs only ones as the entity starting gear to disable any active cloth sensors.
    /// </summary>
    [DataField]
    public bool StartingGear = true;

    /// <summary>
    /// If true, the entity will not register for sensors if an item with suit sensors is equipped.
    /// </summary>
    [DataField]
    public bool RemoveRegistration;
}
