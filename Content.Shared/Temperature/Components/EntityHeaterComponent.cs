using Content.Shared.Temperature.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Temperature.Components;

/// <summary>
/// Adds thermal energy to entities with <see cref="TemperatureComponent"/> placed on it.
/// </summary>
[RegisterComponent, Access(typeof(SharedEntityHeaterSystem))]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EntityHeaterComponent : Component
{
    /// <summary>
    /// Power used when heating at the high setting.
    /// Low and medium are 33% and 66% respectively.
    /// </summary>
    [DataField]
    public float Power = 2400f;

    /// <summary>
    /// Current setting of the heater. If it is off or unpowered it won't heat anything.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityHeaterSetting Setting = EntityHeaterSetting.Off;

    // Frontier: ghost power
    /// <summary>
    /// Passive power draw.  Takes this much while plugged in, even when off.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float PassivePower = 0f;
    // End Frontier

    /// <summary>
    /// An optional sound that plays when the setting is changed.
    /// </summary>
    [DataField]
    public SoundPathSpecifier? SettingSound;
}
