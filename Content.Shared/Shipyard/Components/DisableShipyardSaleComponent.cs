using Content.Shared.Shipyard;

namespace Content.Server._NF.Smuggling.Components;

/// <summary>
///     A component that disables the sale of the ship it's on from a shipyard console.
/// </summary>
[RegisterComponent]
public sealed partial class DisableShipyardSaleComponent : Component
{
    /// <summary>
    ///     The message to print off when a shipyard sale is disabled.
    /// </summary>
    [DataField(required: true)]
    public string Reason;

    /// <summary>
    ///     The console types that should allow selling this object.
    /// </summary>
    [DataField]
    public List<ShipyardConsoleUiKey> AllowedShipyardTypes = new();
}
