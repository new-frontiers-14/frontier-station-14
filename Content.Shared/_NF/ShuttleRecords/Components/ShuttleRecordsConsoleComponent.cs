using Content.Shared._NF.Bank.Components;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._NF.ShuttleRecords.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedShuttleRecordsSystem))]
public sealed partial class ShuttleRecordsConsoleComponent : Component
{
    public static string TargetIdCardSlotId = "ShuttleRecordsConsole-targetId";

    [DataField]
    public ItemSlot TargetIdSlot = new();
    public SoundSpecifier ErrorSound = new SoundPathSpecifier("/Audio/Effects/Cargo/buzz_sigh.ogg");
    public SoundSpecifier ConfirmSound = new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg");

    /// <summary>
    /// This percentage is used to calculate the amount of spesos required to make a new copy using the
    /// shuttle records system. This allows large ships to cost more than smaller ships.
    /// </summary>
    [DataField]
    public double TransactionPercentage = 0.2f;

    /// <summary>
    /// This value is used if the resulting transaction cost is lower than this value.
    /// </summary>
    [DataField]
    public uint MinTransactionPrice = 5000;

    /// <summary>
    /// This value is used if the resulting transaction cost is higher than this value.
    /// </summary>
    [DataField]
    public uint MaxTransactionPrice = 50000;

    /// <summary>
    /// This value is used if it is given, overriding everything.
    /// </summary>
    [DataField]
    public uint? FixedTransactionPrice;

    /// <summary>
    /// The account to withdraw funds from for these services.
    /// </summary>
    [DataField]
    public SectorBankAccount Account;
}
