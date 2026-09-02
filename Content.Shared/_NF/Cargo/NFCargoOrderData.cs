using Robust.Shared.Serialization;

namespace Content.Shared._NF.Cargo;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class NFCargoOrderData
{
    /// <summary>
    /// Price when the order was added.
    /// </summary>
    [DataField]
    public int Price;

    /// <summary>
    /// A unique (arbitrary) ID which identifies this order.
    /// </summary>
    [DataField]
    public int OrderId { get; private set; }

    /// <summary>
    /// Prototype Id for the item to be created
    /// </summary>
    [DataField]
    public string ProductId { get; private set; }

    /// <summary>
    /// Prototype Name
    /// </summary>
    [DataField]
    public string ProductName { get; private set; }

    /// <summary>
    /// The number of items in the order. Not readonly, as it might change
    /// due to caps on the amount of orders that can be placed.
    /// </summary>
    [DataField]
    public int OrderQuantity;

    /// <summary>
    /// How many instances of this order that we've already dispatched
    /// </summary>
    [DataField]
    public int NumDispatched = 0;

    [DataField]
    public string Purchaser { get; private set; }

    [DataField]
    public string Notes { get; private set; }

    [DataField]
    public NetEntity? Computer = null;

    public NFCargoOrderData(int orderId, string productId, string productName, int price, int amount, string purchaser, string notes, NetEntity? computer)
    {
        OrderId = orderId;
        ProductId = productId;
        ProductName = productName;
        Price = price;
        OrderQuantity = amount;
        Purchaser = purchaser;
        Notes = notes;
        Computer = computer;
    }
}
