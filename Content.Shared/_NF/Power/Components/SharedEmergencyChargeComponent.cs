using Robust.Shared.Serialization;

namespace Content.Shared._NF.Power.Components;

/// <summary>
/// Handles station alert level and power changes for emergency lights.
/// All logic is serverside, animation is handled by <see cref="RotatingLightComponent"/>.
/// </summary>
[Access(typeof(SharedEmergencyChargeSystem))]
public abstract partial class SharedEmergencyChargeComponent : Component
{
}

[Serializable, NetSerializable]
public enum EmergencyChargeVisuals
{
    On,
}

public enum EmergencyChargeVisualLayers
{
    Base,
    LightOff,
    LightOn,
}
