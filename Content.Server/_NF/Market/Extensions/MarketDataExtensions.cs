using System.Linq;
using Content.Server._NF.Market.Components;
using Content.Shared._NF.Market;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.Market.Extensions;

public static class MarketDataExtensions
{
    /// <summary>
    /// Update-or-insert the market data list or adds it new if it doesnt exist in there yet.
    /// </summary>
    /// <param name="entityPrototype">The entity prototype to change the amount of.</param>
    /// <param name="increaseAmount">The change in units, ie. 6 plushies.</param>
    /// <param name="marketDataList">The market data list to modify.</param>
    /// <param name="estimatedPrice">The estimated price by the pricing system.</param>
    public static void Upsert(this List<MarketData> marketDataList,
        EntityPrototype entityPrototype,
        int increaseAmount,
        double estimatedPrice,
        string? stackPrototypeId = null)
    {
        // Find the MarketData for the given EntityPrototype.
        var prototypeMarketData = marketDataList.FirstOrDefault(md => md.Prototype == entityPrototype.ID);

        if (prototypeMarketData != null && (prototypeMarketData.Quantity + increaseAmount) >= 0)
        {
            // If it exists, change the count.
            prototypeMarketData.Quantity += increaseAmount;
            if (prototypeMarketData.Quantity <= 0)
            {
                marketDataList.Remove(prototypeMarketData);
            }
        }
        else if (increaseAmount > 0)
        {
            // If it doesn't exist, create a new MarketData and add it to the list.
            marketDataList.Add(new MarketData(entityPrototype.ID, stackPrototypeId ?? prototypeMarketData?.StackPrototype, increaseAmount, estimatedPrice));
        }
    }

    /// <summary>
    /// Get the current maximum amount available for a particular prototype.
    /// </summary>
    /// <param name="marketDataList">the list to check in</param>
    /// <param name="prototype">the prototype to check for</param>
    /// <returns>The max quantity withdrawable</returns>
    public static int GetMaxQuantityToWithdraw(this List<MarketData> marketDataList, EntityPrototype prototype)
    {
        var marketData = marketDataList.FirstOrDefault(md => md.Prototype == prototype.ID);
        return marketData == null ? 0 : marketData.Quantity;
    }

    /// <summary>
    /// Get the total value of the dataList
    /// </summary>
    /// <param name="dataList">The list to get the value of</param>
    /// <param name="marketModifier">The market modifier to apply</param>
    /// <returns>The total value</returns>
    public static int GetMarketValue(List<MarketData> dataList, float marketModifier)
    {
        if (!(dataList.Count >= 1))
            return 0;

        return dataList.Sum(marketData => (int) Math.Round(marketData.Price * marketData.Quantity * marketModifier));
    }
}
