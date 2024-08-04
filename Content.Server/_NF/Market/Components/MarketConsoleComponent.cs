using Content.Server._NF.Market.Systems;
using Content.Shared._NF.Market;
using Content.Shared.Whitelist;

namespace Content.Server._NF.Market.Components;

/// <summary>
/// Component that belongs to the market computer
/// </summary>
[RegisterComponent]
[Access(typeof(MarketSystem))]
public sealed partial class MarketConsoleComponent : Component
{
    [DataField]
    public int MaxCrateMachineDistance = 16;

    public List<MarketData> CartDataList = [];

    /// <summary>
    /// The cost of one transaction.
    /// </summary>
    [DataField]
    public int TransactionCost = 600;
}
