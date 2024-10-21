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
    /// The cost of making a new id card.
    /// This may be zero with different access levels, ie. if the SR uses the console.
    /// </summary>
    [DataField]
    public int TransactionPrice { get; set; } = 10000;
}
