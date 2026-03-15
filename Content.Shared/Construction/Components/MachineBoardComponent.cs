using Content.Shared.Construction.Prototypes; // Frontier: upgradeable components
using Content.Shared.Stacks;
using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary; // Frontier: upgradeable components

namespace Content.Shared.Construction.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class MachineBoardComponent : Component
{
    // Frontier: keep upgradeable components
    /// <summary>
    /// Entities needed to construct this machine, discriminated by component.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<MachinePartPrototype>, int> Requirements = new();
    // End Frontier

    /// <summary>
    /// The stacks needed to construct this machine
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<StackPrototype>, int> StackRequirements = new();

    /// <summary>
    /// Entities needed to construct this machine, discriminated by tag.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<TagPrototype>, GenericPartInfo> TagRequirements = new();

    /// <summary>
    /// Entities needed to construct this machine, discriminated by component.
    /// </summary>
    [DataField]
    public Dictionary<string, GenericPartInfo> ComponentRequirements = new();

    /// <summary>
    /// The machine that's constructed when this machine board is completed.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Prototype;
}

/// <summary>
/// Marker component for any item that's machine board-like without necessarily being a MachineBoardComponent
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CircuitboardComponent : Component;

[DataDefinition, Serializable]
public partial struct GenericPartInfo
{
    [DataField(required: true)]
    public int Amount;

    [DataField(required: true)]
    public EntProtoId DefaultPrototype;

    [DataField]
    public LocId? ExamineName;
}
