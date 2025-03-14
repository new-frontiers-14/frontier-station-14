using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.Mech.Equipment.Events;

[Serializable, NetSerializable]
public sealed partial class ForkInsertDoAfterEvent : SimpleDoAfterEvent;
