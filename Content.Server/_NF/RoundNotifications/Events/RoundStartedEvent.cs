using Robust.Shared.Serialization;

namespace Content.Server._NF.RoundNotifications.Events;

[Serializable, NetSerializable]
public sealed class RoundStartedEvent : EntityEventArgs
{
    public int RoundId { get; }

    public RoundStartedEvent(int roundId)
    {
        RoundId = roundId;
    }
}
