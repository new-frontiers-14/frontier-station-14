using Robust.Shared.Serialization;

namespace Content.Shared._NF.Cargo.BUI;

[NetSerializable, Serializable]
public sealed class NFCargoPalletConsoleInterfaceState(
    int appraisal,
    int count,
    float marketModifier,
    int marketOffer,
    bool enabled) : BoundUserInterfaceState
{
    public NFCargoPalletConsoleInterfaceState() : this(0, 0, 1.0f, 0, false) { }

    /// <summary>
    /// The estimated apraised value of all the entities on top of pallets on the same grid as the console.
    /// </summary>
    public int Appraisal = appraisal;

    /// <summary>
    /// The number of entities on top of pallets on the same grid as the console.
    /// </summary>
    public int Count = count;

    /// <summary>
    /// The market modifier on the cargo console.
    /// </summary>
    public float MarketModifier = marketModifier;

    /// <summary>
    /// The sale price the market offers. This may be different from <code>Appraisal * MarketModifier</code> because
    /// some entities are exempt from market modifiers.
    /// </summary>
    public int MarketOffer = marketOffer;

    /// <summary>
    /// True if the buttons should be enabled.
    /// </summary>
    public bool Enabled = enabled;
}
