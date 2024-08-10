using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;
using Content.Shared.Stacks;

namespace Content.Shared._NF.Market;

[Virtual, NetSerializable, Serializable]
public class MarketData
{
    [ViewVariables]
    public EntProtoId Prototype { get; set; }

    [ViewVariables]
    public ProtoId<StackPrototype>? StackPrototype { get; set; }

    [ViewVariables]
    public int Quantity { get; set; }

    [ViewVariables]
    public double Price { get; set; }

    public MarketData(EntProtoId prototype, ProtoId<StackPrototype>? stackPrototype, int quantity, double price)
    {
        Prototype = prototype;
        StackPrototype = stackPrototype;
        Quantity = quantity;
        Price = price;
    }
}
