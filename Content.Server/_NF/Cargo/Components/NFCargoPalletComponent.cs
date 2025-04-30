using Content.Server.Cargo.Components; // Use upstream BuySellType

namespace Content.Server._NF.Cargo.Components;

/// <summary>
/// Any entities intersecting when a shuttle is recalled will be sold.
/// </summary>

[RegisterComponent]
public sealed partial class NFCargoPalletComponent : Component
{
    /// <summary>
    /// Whether the pad is a buy pad, a sell pad, or all.
    /// </summary>
    [DataField]
    public BuySellType PalletType;
}
