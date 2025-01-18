namespace Content.Shared.Item;

/// <summary>
///     Raised directed at entity being picked up when someone picks it up sucessfully
/// </summary>
public sealed class GettingPickedUpEvent : CancellableEntityEventArgs
{
    public readonly EntityUid User;
    public readonly EntityUid Item;

    public GettingPickedUpEvent(EntityUid user, EntityUid item)
    {
        User = user;
        Item = item;
    }
}
