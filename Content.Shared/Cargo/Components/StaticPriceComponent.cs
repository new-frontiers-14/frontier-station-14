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
    /// Frontier - Edit the vending machine mod to use a diffrent mod, can only be used to add a higher mod then the existing one of the vending machine.
    /// </summary>
    [DataField("vendingPriceModReplacer", required: false)]
    public float VendingPriceModReplacer;
}
