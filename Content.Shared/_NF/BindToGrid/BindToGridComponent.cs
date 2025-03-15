using Robust.Shared.GameStates;

namespace Content.Shared._NF.BindToGrid;

/// <summary>
/// Contains the starting grid of an entity for purposes of binding various functionality to the grid it originated on
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
