namespace Content.Shared._NF.Roles.Events;

/// <summary>
/// Sets an interview applicant's captain approved status.
/// </summary>
public sealed class SetCaptainApprovedEvent(EntityUid captain, bool approved) : EntityEventArgs
{
    public readonly EntityUid Captain = captain;
    public readonly bool Approved = approved;
}
