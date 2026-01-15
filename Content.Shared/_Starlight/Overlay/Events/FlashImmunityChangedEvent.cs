using Robust.Shared.GameObjects;

namespace Content.Shared.Starlight.Overlay;

public sealed class FlashImmunityCheckEvent : EntityEventArgs
{
    public EntityUid EntityUid { get; }
    public bool IsImmune { get; }

    public FlashImmunityCheckEvent(EntityUid entityUid, bool isImmune)
    {
        EntityUid = entityUid;
        IsImmune = isImmune;
    }
}
