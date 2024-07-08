
namespace Content.Server.Shipyard.Components;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ShipyardVoucherComponent : Component
{
    /// <summary>
    ///  Number of redeemable ships that this voucher can still be used for. Decremented on purchase.
    /// </summary>
    [DataField]
    public uint RedemptionsLeft = 1;

    /// <summary>
    ///  If true, card will be destroyed when no redemptions are left. Checked at time of sale.
    /// </summary>
    [DataField]
    public bool DestroyOnEmpty = false;

}
