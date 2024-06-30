using Robust.Shared.Serialization;

namespace Content.Shared._NF.Market.BUI;

[NetSerializable, Serializable]
public sealed class MarketConsoleInterfaceState : BoundUserInterfaceState
{
    /// <summary>
    /// The player's balance
    /// </summary>
    public int Balance;

    /// <summary>
    /// The market modifier to apply on to the price.
    /// 0.1 makes prices 10% of their original value.
    /// </summary>
    public float MarketModifier;

    /// <summary>
    /// Data to display
    /// </summary>
    public List<MarketData> MarketDataList;

    /// <summary>
    /// The currently stored cart data
    /// </summary>
    public List<MarketData> CartDataList;

    /// <summary>
    /// are the buttons enabled
    /// </summary>
    public bool Enabled;

    public MarketConsoleInterfaceState(int balance, float marketModifier, List<MarketData> marketDataList, List<MarketData> cartDataList, bool enabled)
    {
        Balance = balance;
        MarketModifier = marketModifier;
        MarketDataList = marketDataList;
        CartDataList = cartDataList;
        Enabled = enabled;
    }
}
