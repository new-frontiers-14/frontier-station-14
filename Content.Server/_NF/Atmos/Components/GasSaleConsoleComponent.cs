using Content.Server._NF.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.DeviceLinking;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.Atmos.Components;

[RegisterComponent, Access(typeof(GasDepositSystem))]
public sealed partial class GasSaleConsoleComponent : Component
{
    // Source port for commands (query, sell).
    [DataField]
    public ProtoId<SourcePortPrototype> CommandPortName = "GasSaleConsoleSender";

    // Sink port for contents (contents)
    [DataField]
    public ProtoId<SinkPortPrototype> ResponsePortName = "GasSaleConsoleReceiver";

    // Currency type to spawn on sold
    [DataField]
    public ProtoId<StackPrototype> Currency = "Credit";

    [ViewVariables]
    public GasMixture LastKnownMixture = new();
}
