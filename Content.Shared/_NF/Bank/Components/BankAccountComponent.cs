using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Bank.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class BankAccountComponent : Component
{
    // The amount of money this entity has in their bank account.
    // Should not be modified directly, may be out-of-date.
    [DataField("balance", serverOnly: true), Access(typeof(SharedBankSystem))]
    public int Balance;
}
