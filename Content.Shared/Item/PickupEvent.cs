// Fronter Start
namespace Content.Shared.Item;

/// <summary>
///     Raised on a *mob* when it picks something up
/// </summary>
public sealed class PickupEvent : BasePickupAttemptEvent
{
    public PickupEvent(EntityUid user, EntityUid item) : base(user, item) { }
}

/// <summary>
///     Raised directed at entity being picked up when someone picks it up sucessfully
/// </summary>
public sealed class GettingPickedUpEvent : BasePickupEvent
{
    public GettingPickedUpEvent(EntityUid user, EntityUid item) : base(user, item) { }
}

[Virtual]
public class BasePickupEvent : CancellableEntityEventArgs
{
    public readonly EntityUid User;
    public readonly EntityUid Item;

    public BasePickupEvent(EntityUid user, EntityUid item)
    {
        User = user;
        Item = item;
    }
}
// Fronter End
