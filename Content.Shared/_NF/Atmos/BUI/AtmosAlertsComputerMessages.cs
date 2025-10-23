using Robust.Shared.Serialization;

namespace Content.Shared._NF.Atmos.BUI;

[Serializable, NetSerializable]
public sealed class RemoteGasPressurePumpChangePumpDirectionMessage(NetEntity pump, bool inwards)
    : BoundUserInterfaceMessage
{
    public NetEntity Pump { get; } = pump;
    public bool Inwards { get; } = inwards;
}

[Serializable, NetSerializable]
public sealed class RemoteGasPressurePumpChangeOutputPressureMessage(NetEntity pump, float pressure)
    : BoundUserInterfaceMessage
{
    public NetEntity Pump { get; } = pump;
    public float Pressure { get; } = pressure;
}

[Serializable, NetSerializable]
public sealed class RemoteGasPressurePumpToggleStatusMessage(NetEntity pump, bool enabled) : BoundUserInterfaceMessage
{
    public NetEntity Pump { get; } = pump;
    public bool Enabled { get; } = enabled;
}
