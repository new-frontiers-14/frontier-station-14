using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.Pinpointer;

/// <summary>
/// A one-time use object that clears a given pinpointer.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ClearPinpointerComponent : Component
{
    [DataField]
    public LocId? OtherMessage;

    [DataField]
    public TimeSpan ClearTime = TimeSpan.FromSeconds(5);
}

[Serializable, NetSerializable]
public sealed partial class ClearPinpointerDoAfterEvent : SimpleDoAfterEvent;
