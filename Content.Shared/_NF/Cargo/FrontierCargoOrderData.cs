namespace Content.Shared._NF.Cargo;

using Robust.Shared.Serialization;
using System.Text;

[NetSerializable, Serializable]
public sealed class FrontierCargoOrderData(int orderId, string productId, int price, int amount, string requester, string reason)
{
    /// <summary>
    /// Price when the order was added.
    /// </summary>
    public int Price = price;

    /// <summary>
    /// A unique (arbitrary) ID which identifies this order.
    /// </summary>
    public readonly int OrderId = orderId;

    /// <summary>
    /// Prototype Id for the item to be created
    /// </summary>
    public readonly string ProductId = productId;

    /// <summary>
    /// The number of items in the order. Not readonly, as it might change
    /// due to caps on the amount of orders that can be placed.
    /// </summary>
    public int OrderQuantity = amount;

    /// <summary>
    /// How many instances of this order that we've already dispatched
    /// </summary>
    public int NumDispatched = 0;

    public readonly string Requester = requester;

    // public String RequesterRank; // TODO Figure out how to get Character ID card data
    // public int RequesterId;
    public readonly string Reason = reason;
    public bool Approved => Approver is not null;
    public string? Approver;

    public void SetApproverData(string? fullName, string? jobTitle)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            sb.Append($"{fullName} ");
        }

        if (!string.IsNullOrWhiteSpace(jobTitle))
        {
            sb.Append($"({jobTitle})");
        }

        Approver = sb.ToString();
    }
}
