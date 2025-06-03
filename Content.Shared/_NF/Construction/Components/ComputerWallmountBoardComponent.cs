using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._NF.Construction.Components;

/// <summary>
/// Used for construction graphs in building wallmount computers.
/// </summary>
[RegisterComponent]
public sealed partial class ComputerWallmountBoardComponent : Component
{
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? Prototype { get; private set; }
}
