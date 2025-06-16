using System.Numerics;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Stacks;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._NF.Bank.Components;

[RegisterComponent, NetworkedComponent]

public sealed partial class BankATMComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("cashType", customTypeSerializer: typeof(PrototypeIdSerializer<StackPrototype>))]
    public string CashType = "Credit";

    public static string CashSlotId = "bank-ATM-cashSlot";

    // A dictionary of the accounts to credit, and fractions to remove from each deposit.
    [DataField]
    public Dictionary<SectorBankAccount, float> TaxAccounts = new();

    [DataField]
    public ItemSlot CashSlot = new();

    [DataField]
    public SoundSpecifier ErrorSound =
        new SoundPathSpecifier("/Audio/Effects/Cargo/buzz_sigh.ogg");

    [DataField]
    public SoundSpecifier ConfirmSound =
        new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg");
}
