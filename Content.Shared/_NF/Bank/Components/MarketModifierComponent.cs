namespace Content.Shared.Bank.Components;

/// <summary>
/// This is used for applying a pricing modifier to things like vending machines.
/// It's used to ensure that a purchased product costs more than it is actually worth.
/// </summary>
[RegisterComponent]
public sealed partial class MarketModifierComponent : Component
{
    /// <summary>
    /// The amount to multiply an item's price by
    /// </summary>
    [DataField(required: true)]
    public float Mod { get; set; } = 1.0f;

    /// <summary>
    /// True if the modifier is for purchase (e.g. on a vendor)
    /// Currently used for examine strings.
    /// </summary>
    [DataField]
    public bool Buy { get; set; } = true;
}
