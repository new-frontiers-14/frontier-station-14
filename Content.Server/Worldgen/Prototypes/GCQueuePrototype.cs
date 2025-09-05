using Robust.Shared.Prototypes;

namespace Content.Server.Worldgen.Prototypes;

/// <summary>
///     This is a prototype for a GC queue.
/// </summary>
[Prototype("gcQueue")]
public sealed class GCQueuePrototype : IPrototype
{
    /// <inheritdoc />
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    ///     How deep the GC queue is at most. If this value is ever exceeded entities get processed automatically regardless of
    ///     tick-time cap.
    /// </summary>
    [DataField("depth", required: true)]
    public int Depth { get; }

    /// <summary>
    ///     How many miliseconds to spend deleting objects per object in the queue above the MinDepth? Mono Dynamic Queueing
    /// </summary>
    [DataField]
    public double TimeDeletePerObject { get; } = 0.1; // Mono - at 100 objects past the MinDepth will spend up to 10 milliseconds trying to do deletions

    /// <summary>
    ///     The minimum depth before entities in the queue actually get processed for deletion.
    /// </summary>
    [DataField("minDepthToProcess", required: true)]
    public int MinDepthToProcess { get; }

    /// <summary>
    ///     Whether or not the GC should fire an event on the entity to see if it's eligible to skip the queue.
    ///     Useful for making it so only objects a player has actually interacted with get put in the collection queue.
    /// </summary>
    [DataField("trySkipQueue")]
    public bool TrySkipQueue { get; }
}

