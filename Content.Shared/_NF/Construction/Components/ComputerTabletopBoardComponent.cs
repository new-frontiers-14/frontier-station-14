using Robust.Shared.Prototypes;

namespace Content.Shared._NF.Construction.Components;

/// <summary>
/// Used for construction graphs in building tabletop computers.
/// </summary>
[RegisterComponent]
public sealed partial class ComputerTabletopBoardComponent : Component
{
    [DataField]
    public EntProtoId? Prototype { get; private set; }
}
