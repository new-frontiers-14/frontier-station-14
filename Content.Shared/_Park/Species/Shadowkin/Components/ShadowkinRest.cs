using Robust.Shared.GameStates;
using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Park.Species.Shadowkin.Components;


[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ShadowkinRestPowerComponent : Component
{
    // [ViewVariables(VVAccess.ReadOnly)]
    [DataField("isResting")]
    public bool IsResting = false;

    [DataField]
    public EntProtoId RestAction = "ShadowkinRest";

    [DataField("RestActionEntity"), AutoNetworkedField]
    public EntityUid? RestActionEntity;


}

public sealed partial class ShadowkinRestEvent: InstantActionEvent
{

}