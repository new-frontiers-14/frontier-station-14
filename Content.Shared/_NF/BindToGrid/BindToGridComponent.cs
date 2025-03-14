using Robust.Shared.GameStates;

namespace Content.Shared._NF.BindToGrid;

/// <summary>
/// Frontier - Added to AI to allow auto waking up after 5 secs.
/// </summary>
[NetworkedComponent, RegisterComponent]
[AutoGenerateComponentState]
public sealed partial class BindToGridComponent : Component
{
    // the NetUID that this machine/item is bound to
    [DataField]
    [AutoNetworkedField]
    public NetEntity BoundGrid;
}
