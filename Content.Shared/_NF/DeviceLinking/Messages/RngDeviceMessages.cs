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
public enum RngDeviceVisuals
{
    State
}
