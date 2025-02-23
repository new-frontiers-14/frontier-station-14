using Content.Shared.DeviceLinking;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._NF.CrateStorage;

[RegisterComponent, NetworkedComponent]
public sealed partial class CrateStorageComponent: Component
{
    [DataField]
    public ProtoId<SinkPortPrototype> TriggerPort = "Trigger";
}
