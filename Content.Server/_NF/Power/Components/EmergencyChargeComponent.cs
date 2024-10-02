using Content.Server._NF.Power.EntitySystems;
using Content.Shared._NF.Power.Components;

namespace Content.Server._NF.Power.Components;

/// <summary>
///     Component that represents an emergency light, it has an internal battery that charges when the power is on.
/// </summary>
[RegisterComponent, Access(typeof(EmergencyChargeSystem))]
public sealed partial class EmergencyChargeComponent : SharedEmergencyChargeComponent
{
    [ViewVariables]
    public EmergencyChargeState State;

    /// <summary>
    ///     Is this emergency light forced on for some reason and cannot be disabled through normal means
    ///     (i.e. blue alert or higher?)
    /// </summary>
    [DataField]
    public float Wattage = 200;

    [DataField]
    public float ChargingWattage = 200;

    [DataField]
    public float ChargingEfficiency = 0.85f;

    public Dictionary<EmergencyChargeState, string> BatteryStateText = new()
    {
        { EmergencyChargeState.Full, "emergency-light-component-light-state-full" },
        { EmergencyChargeState.Empty, "emergency-light-component-light-state-empty" },
        { EmergencyChargeState.Charging, "emergency-light-component-light-state-charging" },
        { EmergencyChargeState.On, "emergency-light-component-light-state-on" }
    };
}

public enum EmergencyChargeState : byte
{
    Charging,
    Full,
    Empty,
    On
}

public sealed class EmergencyChargeEvent : EntityEventArgs
{
    public EmergencyChargeState State { get; }

    public EmergencyChargeEvent(EmergencyChargeState state)
    {
        State = state;
    }
}
