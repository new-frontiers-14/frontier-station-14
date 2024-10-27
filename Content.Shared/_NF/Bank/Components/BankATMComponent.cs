using Content.Shared.Containers.ItemSlots;
using Content.Shared.Stacks;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Bank.Components;

[RegisterComponent, NetworkedComponent]

public sealed partial class BankATMComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("cashType", customTypeSerializer: typeof(PrototypeIdSerializer<StackPrototype>))]
    public string CashType = "Credit";

    public static string CashSlotId = "bank-ATM-cashSlot";

    // If positive, this fraction will be taken off of any deposits made at this ATM and deposited into the TaxAccount
    [DataField]
    public float TaxCoefficient = 0.0f;

    // The account to deposit taxed funds into.
    [DataField]
    public SectorBankAccount TaxAccount = SectorBankAccount.Frontier;

    [DataField]
    public ItemSlot CashSlot = new();

    [DataField("soundError")]
    public SoundSpecifier ErrorSound =
        new SoundPathSpecifier("/Audio/Effects/Cargo/buzz_sigh.ogg");

    [DataField("soundConfirm")]
    public SoundSpecifier ConfirmSound =
        new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg");
}
