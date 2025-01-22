using Robust.Shared.Prototypes;

namespace Content.Server.Cargo.Components;

// Component to give an object an inflated apprasial price at certain shipyards.
[RegisterComponent]
public sealed partial class ShipmentPriceComponent : Component
{
    /// <summary>
    /// The price of the object at the destination shipyard.
    /// </summary>
    [DataField("bonusPrice", required: true)]
    public int BonusPrice;
}
