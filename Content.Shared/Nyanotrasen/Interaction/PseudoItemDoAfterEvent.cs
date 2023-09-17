using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Pseudo;

[Serializable, NetSerializable]
public sealed partial class PseudoDoAfterEvent : SimpleDoAfterEvent
{
}
