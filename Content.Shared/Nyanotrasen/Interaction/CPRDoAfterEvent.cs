using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Respirator;

[Serializable, NetSerializable]
public sealed partial class CPRDoAfterEvent : SimpleDoAfterEvent
{
}
