using Content.Shared._NF.Trade;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.Trade;

/// <summary>
/// This is used to mark an entity to be used in a trade crates
/// </summary>
[RegisterComponent]
public sealed partial class TradeCrateDestinationComponent : Component
{
    [DataField]
    public ProtoId<TradeCrateDestinationPrototype> DestinationProto;
}
