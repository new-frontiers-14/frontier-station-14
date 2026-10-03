using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.Kitchen;

//Used to handle doAfter for the container dumping into a microwave
[Serializable, NetSerializable]
public sealed partial class MicrowaveInsertDoAfterEvent : SimpleDoAfterEvent
{
}
