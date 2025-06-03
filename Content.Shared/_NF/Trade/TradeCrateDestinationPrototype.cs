using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._NF.Trade;

/// <summary>
/// A data structure that holds relevant
/// information for trade crates (status icons).
/// </summary>
[Prototype]
public sealed partial class TradeCrateDestinationPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The icon that's displayed on the entity.
    /// </summary>
    [DataField(required: true)]
    public SpriteSpecifier Icon = default!;
}
