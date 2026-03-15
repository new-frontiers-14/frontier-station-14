using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Construction.Prototypes; // NOTE: currently exists under base namespace.

/// <summary>
/// This is a prototype for categorizing
/// different types of machine parts.
/// </summary>
[Prototype("machinePart")]
public sealed partial class MachinePartPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// A human-readable name for the machine part type.
    /// </summary>
    [DataField("name")]
    public string Name = string.Empty;

    /// <summary>
    /// A stock part entity based on the machine part.
    /// </summary>
    [DataField("stockPartPrototype", required: true)]
    public EntProtoId StockPartPrototype = string.Empty;

    //Frontier
    /// <summary>
    ///     An icon that will be used to represent this part type.
    /// </summary>
    [DataField]
    public SpriteSpecifier? Icon { get; private set; }
    //End Frontier
}
