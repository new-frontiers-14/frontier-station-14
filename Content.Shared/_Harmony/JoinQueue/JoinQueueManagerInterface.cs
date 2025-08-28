namespace Content.Shared._Harmony.Common.JoinQueue;

/// <summary>
/// Defines the public contract for managing the player join queue.
/// </summary>
public interface IJoinQueueManager
{
    /// <summary>
    /// Initializes the join queue manager.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Gets the total number of players currently waiting in any queue (patron or regular).
    /// </summary>
    int PlayerInQueueCount { get; }

    /// <summary>
    /// Gets the number of players currently considered "in the game" (not in the queue).
    /// </summary>
    int ActualPlayersCount { get; }
}
