using Content.Shared._Harmony.JoinQueue;

namespace Content.Client._Harmony.JoinQueue;

public interface IClientJoinQueueManager : IJoinQueueManager
{
    /// <summary>
    /// The current position of the client in the queue.
    /// </summary>
    int CurrentPosition { get; }

    public event Action? QueueStateUpdated;
}

