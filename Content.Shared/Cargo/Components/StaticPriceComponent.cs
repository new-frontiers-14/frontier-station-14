namespace Content.Shared.Cargo.Components;

/// <summary>
/// This is used for setting a static, unchanging price for an object.
/// </summary>
[RegisterComponent]
public sealed partial class StaticPriceComponent : Component
{
    /// <summary>
    /// The price of the object this component is on.
    /// </summary>
    [DataField("price", required: true)]
    public double Price;

    /// <summary>
    /// Frontier - An extra mod price for the object, added before the vending machine mod to allow a lower selling price compared to the vending machine price.
    /// </summary>
    [DataField("vendingPriceMod", required: false)]
    public float VendingPriceMod;
}
