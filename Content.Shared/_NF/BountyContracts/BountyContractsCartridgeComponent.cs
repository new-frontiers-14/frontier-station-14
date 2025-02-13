using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._NF.BountyContracts;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, Access(typeof(SharedBountyContractSystem))]
public sealed partial class BountyContractsCartridgeComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<BountyContractCollectionPrototype>? Collection = null;
}
