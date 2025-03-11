using Robust.Shared.Serialization;

namespace Content.Shared._NF.DeviceLinking;

[Serializable, NetSerializable]
public sealed class RngDeviceToggleMuteMessage : BoundUserInterfaceMessage
{
    public bool Muted { get; }

    public RngDeviceToggleMuteMessage(bool muted)
    {
        Muted = muted;
    }
}

[Serializable, NetSerializable]
public sealed class RngDeviceToggleEdgeModeMessage : BoundUserInterfaceMessage
{
    public bool EdgeMode { get; }

    public RngDeviceToggleEdgeModeMessage(bool edgeMode)
    {
        EdgeMode = edgeMode;
    }
}

[Serializable, NetSerializable]
public sealed class RngDeviceSetTargetNumberMessage : BoundUserInterfaceMessage
{
    public int TargetNumber { get; }

    public RngDeviceSetTargetNumberMessage(int targetNumber)
    {
        TargetNumber = targetNumber;
    }
}

[Serializable, NetSerializable]
public enum RngDeviceUiKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class RngDeviceBoundUserInterfaceState : BoundUserInterfaceState
{
    public bool Muted { get; }
    public int TargetNumber { get; }
    public int Outputs { get; }
    public bool EdgeMode { get; }
    public string DeviceType { get; }

    public RngDeviceBoundUserInterfaceState(bool muted, int targetNumber, int outputs, bool edgeMode)
    {
        Muted = muted;
        TargetNumber = targetNumber;
        Outputs = outputs;
        EdgeMode = edgeMode;
        DeviceType = outputs switch
        {
            2 => "Percentile",
            4 => "D4",
            6 => "D6",
            8 => "D8",
            10 => "D10",
            12 => "D12",
            20 => "D20",
            _ => "Unknown"
        };
    }

    public RngDeviceBoundUserInterfaceState(bool muted, int targetNumber, int outputs, bool edgeMode, string deviceType)
    {
        Muted = muted;
        TargetNumber = targetNumber;
        Outputs = outputs;
        EdgeMode = edgeMode;
        DeviceType = deviceType;
    }
}

[Serializable, NetSerializable]
public enum RngDeviceVisuals
{
    State
}
