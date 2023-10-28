using Robust.Shared.Serialization;

namespace Content.Shared._Park.Species.Shadowkin.Events;

/// <summary>
///     Raised to notify other systems of an attempt to blackeye a shadowkin.
/// </summary>
public sealed class ShadowkinBlackeyeAttemptEvent : CancellableEntityEventArgs
{
    public readonly EntityUid Uid;

    public ShadowkinBlackeyeAttemptEvent(EntityUid uid)
    {
        Uid = uid;
    }
}

/// <summary>
///     Raised when a shadowkin becomes a blackeye.
/// </summary>
// [Serializable, NetSerializable]
public sealed class ShadowkinBlackeyeEvent : EntityEventArgs
{
    public readonly EntityUid Uid;
    public readonly bool Damage;

    public ShadowkinBlackeyeEvent(EntityUid uid, bool damage = true)
    {
        Uid = uid;
        Damage = damage;
    }
}
