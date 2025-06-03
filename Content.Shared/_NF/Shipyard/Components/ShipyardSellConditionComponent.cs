namespace Content.Shared._NF.Shipyard.Components;

/// <summary>
///     A component that disables the sale of the ship it's on from a shipyard console.
/// </summary>
[RegisterComponent]
public sealed partial class ShipyardSellConditionComponent : Component
{
    /// <summary>
    ///     Whether this item is preserved on shipyard sale.
    /// </summary>
    [DataField]
    public bool PreserveOnSale = false;

    /// <summary>
    ///     Whether this item prevents shipyard sale.
    /// </summary>
    [DataField]
    public bool BlockSale = false;

    /// <summary>
    ///     The message to print off when a shipyard sale is disabled.
    /// </summary>
    [DataField]
    public LocId? Reason;

    /// <summary>
    ///     The console types that should allow selling this object if BlockSale is true.
    /// </summary>
    [DataField]
    public List<ShipyardConsoleUiKey> AllowedShipyardTypes = new();


}
