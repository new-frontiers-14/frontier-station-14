using Robust.Shared.Serialization;

namespace Content.Shared.DeviceLinking;

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
public enum RngDeviceUiKey
{
    Key
}
