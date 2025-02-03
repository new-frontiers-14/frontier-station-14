namespace Content.Server._NF.CrateMachine;

/// <summary>
/// Raised whenever a crate machine state changes.
/// Only used on server side.
/// </summary>
public sealed class CrateMachineOpenedEvent : EntityEventArgs
{
    public EntityUid EntityUid { get; }

    public CrateMachineOpenedEvent(EntityUid uid)
    {
        EntityUid = uid;
    }
}
