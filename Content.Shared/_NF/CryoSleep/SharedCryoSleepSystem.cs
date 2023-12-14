using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.CryoSleep;

public abstract partial class SharedCryoSleepSystem : EntitySystem
{
    [Serializable, NetSerializable]
    public sealed partial class CryoStoreDoAfterEvent : SimpleDoAfterEvent
    {
    }
}
