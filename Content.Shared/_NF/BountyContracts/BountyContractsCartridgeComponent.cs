using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._NF.BountyContracts;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
[AutoGenerateComponentState, Access(typeof(SharedBountyContractSystem))]
public sealed partial class BountyContractsCartridgeComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<BountyContractCollectionPrototype>? Collection = null;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public bool CreateEnabled;

    [ViewVariables(VVAccess.ReadWrite), DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextCreate;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float CreateCooldown = 20f;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public bool NotificationsEnabled = true;
}
