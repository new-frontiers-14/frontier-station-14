using Content.Shared._NF.Trade;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.Trade;

/// <summary>
/// This is used to mark an entity to be used as a destination for trade crates.
/// </summary>
[RegisterComponent]
public sealed partial class TradeCrateDestinationComponent : Component
{
    [DataField(required: true)]
    public ProtoId<TradeCrateDestinationPrototype> DestinationProto;
}
