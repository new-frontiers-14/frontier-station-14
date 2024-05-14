using Robust.Shared.Serialization;

namespace Content.Shared.Bank.Events;

/// <summary>
/// Raised on a client bank withdrawl
/// </summary>
[Serializable, NetSerializable]

public sealed class StationBankWithdrawMessage : BoundUserInterfaceMessage
{
    //amount to withdraw. validation is happening server side but we still need client input from a text field.
    public ulong Amount;
    public string? Reason;
    public string? Description;
    public StationBankWithdrawMessage(ulong amount, string? reason, string? description)
    {
        Amount = amount;
        Reason = reason;
        Description = description;
    }
}
