using Robust.Shared.Serialization;

namespace Content.Shared._NF.Pirate.BUI;

[NetSerializable, Serializable]
public sealed class PiratePalletConsoleInterfaceState : BoundUserInterfaceState
{
    /// <summary>
    /// number of entities on top of pallets on the same grid as the console
    /// </summary>
    public int Count;

    /// <summary>
    /// are the buttons enabled
    /// </summary>
    public bool Enabled;

    public PiratePalletConsoleInterfaceState(int count, bool enabled)
    {
        Count = count;
        Enabled = enabled;
    }
}
