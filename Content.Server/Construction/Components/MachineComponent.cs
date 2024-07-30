using Content.Shared.Construction.Components;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.Construction.Components;

[RegisterComponent]
public sealed partial class MachineComponent : Component
{
    [DataField]
    public EntProtoId<MachineBoardComponent>? Board { get; private set; }

    [ViewVariables]
    public Container BoardContainer = default!;
    [ViewVariables]
    public Container PartContainer = default!;
}

// Frontier: maintain upgradeable machine parts
/// <summary>
/// The different types of scaling that are available for machine upgrades
/// </summary>
public enum MachineUpgradeScalingType : byte
{
    Linear,
    Exponential
}
// End Frontier
