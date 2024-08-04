using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.SectorServices.Prototypes;

/// <summary>
/// Prototype that represents game entities.
/// </summary>
// Do we need the NetSerializable attribute?
[Prototype]
public sealed partial class SectorServicePrototype : IPrototype
{
    /// <summary>
    /// The "in code name" of the object. Must be unique.
    /// </summary>
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// A dictionary mapping a service to its necessary components.
    /// </summary>
    [DataField]
    public ComponentRegistry Components { get; } = new();
}
