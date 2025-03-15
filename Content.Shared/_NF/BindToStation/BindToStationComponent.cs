using Robust.Shared.GameStates;

namespace Content.Shared._NF.BindToStation;

/// <summary>
/// Contains the starting station of an entity for purposes of binding various functionality to the station it originated on
/// </summary>
[NetworkedComponent, RegisterComponent]
[AutoGenerateComponentState]
public sealed partial class BindToStationComponent : Component
{
    // the entity of the station that this machine/item is bound to
    [DataField]
    [AutoNetworkedField]
    public EntityUid? BoundStation;
}
