using Robust.Shared.Network;

namespace Content.Server.CryoSleep;

public abstract class BaseCryosleepEvent : EntityEventArgs
{
    public NetUserId? User;
    public EntityUid Cryopod;

    protected BaseCryosleepEvent(EntityUid cryopod, NetUserId? user)
    {
        Cryopod = cryopod;
        User = user;
    }
}

/// <summary>
///   Raised on an entity who has entered cryosleep.
/// </summary>
public sealed class CryosleepEnterEvent : BaseCryosleepEvent
{
    public CryosleepEnterEvent(EntityUid cryopod, NetUserId? user) : base(cryopod, user) {}
}

/// <summary>
///   Raised on an entity who has successfully woken up from cryosleep.
/// </summary>
public sealed class CryosleepWakeUpEvent : BaseCryosleepEvent
{
    public CryosleepWakeUpEvent(EntityUid cryopod, NetUserId? user) : base(cryopod, user) {}
}

/// <summary>
///   Raised on an entity who is going to enter cryosleep before their mind is detached.
/// </summary>
public sealed class CryosleepBeforeMindRemovedEvent : BaseCryosleepEvent
{
    public CryosleepBeforeMindRemovedEvent(EntityUid cryopod, NetUserId? user) : base(cryopod, user) {}
}
