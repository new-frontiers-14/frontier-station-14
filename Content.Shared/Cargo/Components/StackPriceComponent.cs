namespace Content.Shared.Cargo.Components;

/// <summary>
/// This is used for pricing stacks of items.
/// </summary>
[RegisterComponent]
public sealed partial class StackPriceComponent : Component
{
    /// <summary>
    /// The price of the object this component is on, per unit.
    /// </summary>
    [DataField("price", required: true)]
    public double Price;
    /// <summary>
    /// The price a full stack of this object sells for from a vendor.
    /// </summary>
    [DataField]
    public double VendPrice;
}
