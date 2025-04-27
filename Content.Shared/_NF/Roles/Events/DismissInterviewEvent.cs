namespace Content.Shared._NF.Roles.Events;

/// <summary>
/// Tries to dismiss a given interview.
/// </summary>
public sealed class DismissInterviewEvent(EntityUid captain, bool reopenSlot) : EntityEventArgs
{
    public readonly EntityUid Captain = captain;
    public readonly bool ReopenSlot = reopenSlot;
}
