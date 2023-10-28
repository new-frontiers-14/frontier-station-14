using Robust.Shared.Serialization;

namespace Content.Shared._Park.Species.Shadowkin.Events;

/// <summary>
///     Raised over network to notify the client that they're going in/out of The Dark.
/// </summary>
[Serializable, NetSerializable]
public sealed class ShadowkinDarkSwappedEvent : EntityEventArgs
{
    public NetEntity? Performer { get; set; }
    // public EntityUid Performer { get; }
    public bool DarkSwapped { get; }

    public ShadowkinDarkSwappedEvent(NetEntity performer, bool darkSwapped)
    {
        Performer = performer;
        DarkSwapped = darkSwapped;
    }
}
