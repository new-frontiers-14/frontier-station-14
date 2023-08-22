namespace Content.Shared._NF.Cargo.Components;

/// <summary>
/// This is used for setting a static, unchanging price for buying an object from a vending machine.
/// </summary>
[RegisterComponent]
public sealed class VendPriceComponent : Component
{
    /// <summary>
    /// The price of the object this component is on.
    /// </summary>
    [DataField("price", required: true)]
    public double Price;
}
