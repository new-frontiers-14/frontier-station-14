namespace Content.Shared._NF.Roles.Events;

/// <summary>
/// Tries to dismiss a given interview.
/// </summary>
public sealed class DismissInterviewEvent(EntityUid dismisser, bool reopenSlot) : EntityEventArgs
{
    /// <summary>
    /// The person requesting the dismissal.
    /// </summary>
    public readonly EntityUid Dismisser = dismisser;

    /// <summary>
    /// If true, the slot for the job should be reopened.
    /// </summary>
    public readonly bool ReopenSlot = reopenSlot;
}
