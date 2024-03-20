using Robust.Shared.Serialization;

namespace Content.Shared._NF.Cargo.BUI;

[NetSerializable, Serializable]
public sealed class FrontierCargoPalletConsoleInterfaceState(int appraisal, int count, bool enabled)
    : BoundUserInterfaceState
{
    /// <summary>
    /// estimated appraised value of all the entities on top of pallets on the same grid as the console
    /// </summary>
    public int Appraisal = appraisal;

    /// <summary>
    /// number of entities on top of pallets on the same grid as the console
    /// </summary>
    public int Count = count;

    /// <summary>
    /// are the buttons enabled
    /// </summary>
    public bool Enabled = enabled;
}
