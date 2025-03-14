using Robust.Shared.GameStates;

namespace Content.Shared._NF.BindToGrid;

/// <summary>
/// Frontier - Added to AI to allow auto waking up after 5 secs.
/// </summary>
[NetworkedComponent, RegisterComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause(Dirty = true)]
public sealed partial class BindToGridComponent : Component
{
    // The length of time, in seconds, to sleep
    [DataField]
    public NetEntity BoundGrid;
}
