using Content.Server._NF.Market.Systems;
using Content.Shared._NF.Market;

namespace Content.Server._NF.Market.Components;

/// <summary>
/// Component that is put on the console's grid that will hold all things that are sold at cargo, for that grid.
/// </summary>
[RegisterComponent]
[Access(typeof(MarketSystem))]
public sealed partial class MarketDataComponent : Component
{
    public List<MarketData> MarketDataList = [];
}
