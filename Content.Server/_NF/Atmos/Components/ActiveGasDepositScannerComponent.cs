namespace Content.Server._NF.Atmos.Components;

/// <summary>
/// Used to keep track of which gas deposit scanners are active.
/// </summary>
[RegisterComponent]
public sealed partial class ActiveGasDepositScannerComponent : Component
{
    // Set to a tiny bit after the default because otherwise the user often gets a blank window when first using
    [DataField]
    public float AccumulatedFrametime = 2.01f;

    /// <summary>
    /// How often to update the gas deposit scanner, in seconds.
    /// </summary>
    [DataField]
    public float UpdateInterval = 1f;
}
