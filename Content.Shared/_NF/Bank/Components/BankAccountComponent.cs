using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Bank.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class BankAccountComponent : Component
{
    // The amount of money this entity has in their bank account.
    // Should not be modified directly, may be out-of-date.
    [DataField("balance"), Access(typeof(SharedBankSystem))]
    [AutoNetworkedField]
    public int Balance;
}
