using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.CryoSleep.Events;

/// <summary>
///   Raised on a cryopod that an entity was shoved into,
///   only if the mind of that entity has not decided to proceed with cryosleep or cancel it.
///
///   The target and the user of this event is the cryopod, and the "used" is the body put into it.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class CryoStoreDoAfterEvent : SimpleDoAfterEvent;
