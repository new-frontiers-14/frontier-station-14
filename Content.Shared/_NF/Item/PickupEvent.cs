namespace Content.Shared._NF.Item;

/// <summary>
///     Raised directed at entity being picked after someone picks it up sucessfully.
/// </summary>
public sealed class PickedUpEvent : EntityEventArgs
{
    public readonly EntityUid User;
    public readonly EntityUid Item;

    public PickedUpEvent(EntityUid user, EntityUid item)
    {
        User = user;
        Item = item;
    }
}
