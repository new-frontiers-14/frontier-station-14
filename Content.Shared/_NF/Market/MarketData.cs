using Robust.Shared.Serialization;

namespace Content.Shared._NF.Market;

[Virtual, NetSerializable, Serializable]
public class MarketData
{
    public string Prototype { get; set; }
    public int Quantity { get; set; }

    public double Price { get; set; }

    public MarketData(string prototype, int quantity, double price)
    {
        Prototype = prototype;
        Quantity = quantity;
        Price = price;
    }
}
