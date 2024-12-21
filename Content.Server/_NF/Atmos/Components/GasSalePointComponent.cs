using Content.Server._NF.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.Atmos.Components;

[RegisterComponent, Access(typeof(GasDepositSystem))]
public sealed partial class GasSalePointComponent : Component
{
    [DataField]
    public string InletPipePortName = "inlet";

    // An unlimited internal gas storage, tracking how much gas has been put into the entity.
    [ViewVariables]
    public GasMixture GasStorage = new();

    // Sink port for clear commands.
    [DataField]
    public ProtoId<SinkPortPrototype> CommandPortName = "GasSalePointReceiver";

    // Source port for responses.
    [DataField]
    public ProtoId<SourcePortPrototype> ResponsePortName = "GasSalePointSender";
}
