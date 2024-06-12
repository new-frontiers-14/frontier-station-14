using Robust.Shared.Serialization;

namespace Content.Shared.Bank.BUI;

[NetSerializable, Serializable]
public sealed class StationBankATMMenuInterfaceState : BoundUserInterfaceState
{
    /// <summary>
    /// bank balance of the character using the atm
    /// </summary>
    public ulong Balance;

    /// <summary>
    /// are the buttons enabled
    /// </summary>
    public bool Enabled;

    /// <summary>
    /// how much cash is inserted
    /// </summary>
    public ulong Deposit;

    public StationBankATMMenuInterfaceState(ulong balance, bool enabled, ulong deposit)
    {
        Balance = balance;
        Enabled = enabled;
        Deposit = deposit;
    }
}
