using System.Linq;
using Content.Shared._NF.Market;

namespace Content.Server._NF.Market.Extensions;

public static class MarketDataExtensions
{
    /// <summary>
    /// Update-or-insert the market data list or adds it new if it doesnt exist in there yet.
    /// </summary>
    /// <param name="entityPrototypeId">The entity prototype id to change the amount of.</param>
    /// <param name="increaseAmount">The change in units, ie. 6 plushies.</param>
    /// <param name="marketDataList">The market data list to modify.</param>
    public static void Upsert(this List<MarketData> marketDataList, string entityPrototypeId, int increaseAmount)
    {
        // Find the MarketData for the given EntityPrototype.
        var prototypeMarketData = marketDataList.FirstOrDefault(md => md.Prototype == entityPrototypeId);

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
            marketDataList.Add(new MarketData(entityPrototypeId, increaseAmount));
        }
    }
}
