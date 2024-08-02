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
    public string CashType = "Credit";

    [DataField]
    public int MaxCrateMachineDistance = 16;

    public List<MarketData> CartDataList = [];

    /// <summary>
    /// Whitelist for this console, will only show items described in the whitelist.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Blacklist for this console, will never show items described in the blacklist.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;
}
