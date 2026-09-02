using Robust.Shared.Serialization;

namespace Content.Shared._NF.Cargo.BUI;

[NetSerializable, Serializable]
public sealed class NFCargoPalletConsoleInterfaceState(
    int appraisal,
    int count,
    bool enabled) : BoundUserInterfaceState
{
    /// <summary>
    /// The estimated apraised value of all the entities on top of pallets on the same grid as the console.
    /// </summary>
    public int Appraisal = appraisal;

    /// <summary>
    /// The number of entities on top of pallets on the same grid as the console.
    /// </summary>
    public int Count = count;

    /// <summary>
    /// True if the buttons should be enabled.
    /// </summary>
    public bool Enabled = enabled;
}
