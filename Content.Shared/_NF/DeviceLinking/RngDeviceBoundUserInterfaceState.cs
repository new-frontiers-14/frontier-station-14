using Robust.Shared.Serialization;

namespace Content.Shared.DeviceLinking;

[Serializable, NetSerializable]
public sealed class RngDeviceBoundUserInterfaceState : BoundUserInterfaceState
{
    public bool IsMuted { get; }

    public RngDeviceBoundUserInterfaceState(bool isMuted)
    {
        IsMuted = isMuted;
    }
}
