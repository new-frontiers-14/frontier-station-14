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
    /// <summary>
    /// The message to print when there are no charges left on the item.
    /// </summary>
    [DataField]
    public LocId? EmptyMessage;

    /// <summary>
    /// The message to print when the item is used on somebody else.
    /// </summary>
    [DataField]
    public LocId? UseOnOthersMessage;

    /// <summary>
    /// The message to print when the item is used on yourself.
    /// </summary>
    [DataField]
    public LocId? UseOnSelfMessage;

    /// <summary>
    /// The amount of time it takes to clear an item's pinpointer status.
    /// </summary>
    [DataField]
    public TimeSpan ClearTime = TimeSpan.FromSeconds(5);

    /// <summary>
    /// If true, destroys an item after it's used.
    /// </summary>
    [DataField]
    public bool DestroyAfterUse;
}

[Serializable, NetSerializable]
public sealed partial class ClearPinpointerDoAfterEvent : SimpleDoAfterEvent;
