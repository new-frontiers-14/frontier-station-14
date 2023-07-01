using Robust.Shared.Serialization;

namespace Content.Shared.Bank.BUI;

[NetSerializable, Serializable]
public sealed class StationBankATMMenuInterfaceState : BoundUserInterfaceState
{
    /// <summary>
    /// bank balance of the character using the atm
    /// </summary>
    public int Balance;

    /// <summary>
    /// are the buttons enabled
    /// </summary>
    public bool Enabled;

    public StationBankATMMenuInterfaceState(int balance, bool enabled)
    {
        Balance = balance;
        Enabled = enabled;
    }
}
