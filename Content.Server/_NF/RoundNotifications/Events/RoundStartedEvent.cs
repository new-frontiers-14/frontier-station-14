namespace Content.Server._NF.RoundNotifications.Events;

[Serializable]
public sealed class RoundStartedEvent : EntityEventArgs
{
    public int RoundId { get; }

    public RoundStartedEvent(int roundId)
    {
        RoundId = roundId;
    }
}
