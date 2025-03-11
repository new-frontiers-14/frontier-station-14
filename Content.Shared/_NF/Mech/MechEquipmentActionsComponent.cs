using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._NF.Mech.Components;

/// <summary>
/// A set of actions for 
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MechEquipmentActionsComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<EntProtoId> Actions = new();

    [DataField, AutoNetworkedField]
    public Dictionary<EntProtoId, EntityUid?> ActionEntities = new();
}
