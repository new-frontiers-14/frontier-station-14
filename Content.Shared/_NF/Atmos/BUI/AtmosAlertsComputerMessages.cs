using Robust.Shared.Serialization;

namespace Content.Shared._NF.Atmos.BUI;

[Serializable, NetSerializable]
public sealed class RemoteGasPressurePumpChangePumpDirectionMessage : BoundUserInterfaceMessage
{
    public NetEntity Pump { get; }
    public bool Inwards { get; }

    public RemoteGasPressurePumpChangePumpDirectionMessage(NetEntity pump, bool inwards)
    {
        Pump = pump;
        Inwards = inwards;
    }
}

[Serializable, NetSerializable]
public sealed class RemoteGasPressurePumpChangeOutputPressureMessage : BoundUserInterfaceMessage
{
    public NetEntity Pump { get; }
    public float Pressure { get; }

    public RemoteGasPressurePumpChangeOutputPressureMessage(NetEntity pump, float pressure)
    {
        Pump = pump;
        Pressure = pressure;
    }
}

[Serializable, NetSerializable]
public sealed class RemoteGasPressurePumpToggleStatusMessage : BoundUserInterfaceMessage
{
    public NetEntity Pump { get; }
    public bool Enabled { get; }

    public RemoteGasPressurePumpToggleStatusMessage(NetEntity pump, bool enabled)
    {
        Pump = pump;
        Enabled = enabled;
    }
}
