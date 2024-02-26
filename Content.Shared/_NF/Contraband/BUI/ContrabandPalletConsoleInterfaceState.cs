using Robust.Shared.Serialization;

namespace Content.Shared._NF.Contraband.BUI;

[NetSerializable, Serializable]
public sealed class ContrabandPalletConsoleInterfaceState : BoundUserInterfaceState
{
    /// <summary>
    /// estimated appraised value of all the contraband on top of pallets on the same grid as the console
    /// </summary>
    public int Appraisal;

    /// <summary>
    /// number of contraband entities on top of pallets on the same grid as the console
    /// </summary>
    public int Count;

    /// <summary>
    /// are the buttons enabled
    /// </summary>
    public bool Enabled;

    public ContrabandPalletConsoleInterfaceState(int appraisal, int count, bool enabled)
    {
        Appraisal = appraisal;
        Count = count;
        Enabled = enabled;
    }
}
