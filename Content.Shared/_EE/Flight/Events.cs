using Robust.Shared.Serialization;
using Content.Shared.DoAfter;

namespace Content.Shared._EE.Flight.Events;

[Serializable, NetSerializable]
public sealed partial class FlightDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed class FlightEvent(NetEntity uid, bool isFlying, bool isAnimated) : EntityEventArgs
{
    public NetEntity Uid { get; } = uid;
    public bool IsFlying { get; } = isFlying;
    public bool IsAnimated { get; } = isAnimated;
}