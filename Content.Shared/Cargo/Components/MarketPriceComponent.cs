namespace Content.Shared.Cargo.Components;

/// <summary>
/// This is used for setting a static, unchanging price for selling an object.
/// </summary>
[RegisterComponent]
public sealed class MarketPriceComponent : Component
{
    /// <summary>
    /// The price of the object this component is on.
    /// </summary>
    [DataField("price", required: true)]
    public double Price;
}
