using Robust.Shared.GameStates;

namespace Content.Shared._NF.Pinpointer;

/// <summary>
/// An object that is being tracked by pinpointers.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PinpointerTargetComponent : Component
{
    /// <summary>
    /// The list of entities that's currently tracking this object.
    /// State should only be reliable serverside.
    /// </summary>
    [DataField]
    public List<EntityUid> Entities = new();
}
