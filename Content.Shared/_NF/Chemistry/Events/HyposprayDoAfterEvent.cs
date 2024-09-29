using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.Chemistry.Events;

[Serializable, NetSerializable]
public sealed partial class HyposprayDoAfterEvent : SimpleDoAfterEvent
{
}