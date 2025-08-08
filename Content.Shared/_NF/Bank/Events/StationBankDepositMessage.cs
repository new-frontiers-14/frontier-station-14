using Robust.Shared.Serialization;

namespace Content.Shared._NF.Bank.Events;

/// <summary>
/// Raised on a client bank deposit
/// </summary>
[Serializable, NetSerializable]

public sealed class StationBankDepositMessage : BoundUserInterfaceMessage
{
    //amount to deposit. validation is happening server side but we still need client input from a text field.
    public int Amount;
    public string? Reason;
    public string? Description;
    public StationBankDepositMessage(int amount, string? reason, string? description)
    {
        Amount = amount;
        Reason = reason;
        Description = description;
    }
}
