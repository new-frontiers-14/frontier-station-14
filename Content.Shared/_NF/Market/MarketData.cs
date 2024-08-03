using Robust.Shared.Serialization;

namespace Content.Shared._NF.Market;

[Virtual, NetSerializable, Serializable]
public class MarketData
{
    public string Prototype { get; set; }

    public string? StackPrototype { get; set; }
    public int Quantity { get; set; }

    public double Price { get; set; }

    public MarketData(string prototype, string? stackPrototype, int quantity, double price)
    {
        Prototype = prototype;
        StackPrototype = stackPrototype;
        Quantity = quantity;
        Price = price;
    }
}
