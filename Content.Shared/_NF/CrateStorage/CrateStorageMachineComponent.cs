using Content.Shared.DeviceLinking;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._NF.CrateStorage;

[RegisterComponent, NetworkedComponent]
public sealed partial class CrateStorageMachineComponent: Component
{
    [DataField]
    public ProtoId<SinkPortPrototype> TriggerPort = "Trigger";

    /// <summary>
    /// Capacity of the crate storage
    /// Set to 0 for unlimited capacity
    /// </summary>
    [DataField]
    public int Capacity = 4;
}
