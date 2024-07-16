using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.SectorServices.Prototypes;

/// <summary>
/// Prototype that represents game entities.
/// </summary>
[Prototype("sectorService")]
public sealed partial class SectorServicePrototype : IPrototype
{
    /// <summary>
    /// The "in code name" of the object. Must be unique.
    /// </summary>
    [ViewVariables]
    [IdDataFieldAttribute]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// A dictionary mapping the component type list to the YAML mapping containing their settings.
    /// </summary>
    [DataField]
    public ComponentRegistry Components { get; } = new();
}
