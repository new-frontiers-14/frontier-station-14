using Content.Server._NF.Market.Systems;
using Content.Shared._NF.Market;
using Content.Shared.Whitelist;

namespace Content.Server._NF.Market.Components;

/// <summary>
/// Component that is put on the console's grid that will hold all things that are sold at cargo, for that grid.
/// </summary>
[RegisterComponent]
[Access(typeof(MarketSystem))]
public sealed partial class CargoMarketDataComponent : Component
{
    [DataField]
    public List<MarketData> MarketDataList = [];

    /// <summary>
    /// Sold items must match this whitelist to enter into this data set.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Sold items not must match this blacklist to enter into this data set.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// Particular items that may override the blacklist.
    /// </summary>
    [DataField]
    public EntProtoIdWhitelist? OverrideList;
}
