namespace Content.Shared._NF.Whitelist.Components;

/// <summary>
/// Whitelist component for shipyard vouchers to avoid tag redefinition and collisions
/// </summary>
/// <remarks>
/// FIXME: move ShipyardVoucher definition to shared to prevent mispredicts.    
/// </remarks>
[RegisterComponent]
public sealed partial class NFShipyardVoucherComponent : Component;
