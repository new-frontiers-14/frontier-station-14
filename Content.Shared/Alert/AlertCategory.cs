namespace Content.Shared.Alert;

/// <summary>
/// Every category of alert. Corresponds to category field in alert prototypes defined in YML
/// </summary>
public enum AlertCategory
{
    Pressure,
    Temperature,
    Breathing,
    Buckled,
    Health,
    Internals,
    Stamina,
    ShadowkinPower,
    Piloting,
    Hunger,
    Thirst,
    Toxins,
    Battery
}
