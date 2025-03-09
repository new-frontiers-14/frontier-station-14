using Content.Shared.Containers.ItemSlots;
using Content.Shared.Stacks;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._NF.Bank.Components;

[RegisterComponent, NetworkedComponent]

public sealed partial class StationBankATMComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("cashType", customTypeSerializer:typeof(PrototypeIdSerializer<StackPrototype>))]
    public string CashType = "Credit";

    public static string CashSlotId = "station-bank-ATM-cashSlot";

    [DataField]
    public ItemSlot CashSlot = new();

    [DataField]
    public SectorBankAccount Account = SectorBankAccount.Invalid;

    [DataField("soundError")]
    public SoundSpecifier ErrorSound =
        new SoundPathSpecifier("/Audio/Effects/Cargo/buzz_sigh.ogg");

    [DataField("soundConfirm")]
    public SoundSpecifier ConfirmSound =
        new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg");
}

public enum SectorBankAccount : byte
{
    Invalid, // No assigned account.
    Frontier,
    Nfsd,
    Medical,
}
