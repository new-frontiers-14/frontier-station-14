using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._NF.Trade;

/// <summary>
/// A data structure that holds relevant
/// information for trade crates (status icons).
/// </summary>
[DataDefinition, Prototype("tradeCrateDestination")]
public sealed partial class TradeCrateDestinationPrototype : IPrototype
{
    /// <summary>
    /// The identifier of this prototype
    /// </summary>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The icon that's displayed on the entity.
    /// </summary>
    [DataField(required: true)]
    public SpriteSpecifier Icon = default!;
}
