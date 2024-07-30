using Content.Server._NF.Market.Systems;
using Content.Shared._NF.Market;

namespace Content.Server._NF.Market.Components;

/// <summary>
/// Component that belongs to the market computer
/// </summary>
[RegisterComponent]
[Access(typeof(MarketSystem))]
public sealed partial class MarketDataComponent : Component
{
    public List<MarketData> MarketDataList = [];
}
