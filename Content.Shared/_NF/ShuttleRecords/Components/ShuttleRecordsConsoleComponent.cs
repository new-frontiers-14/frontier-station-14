using Content.Shared.Access;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._NF.ShuttleRecords.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedShuttleRecordsSystem))]
public sealed partial class ShuttleRecordsConsoleComponent : Component
{
    public static string TargetIdCardSlotId = "ShuttleRecordsConsole-targetId";

    [DataField("targetIdSlot")]
    public ItemSlot TargetIdSlot = new();

    [DataField("soundError")]
    public SoundSpecifier ErrorSound =
        new SoundPathSpecifier("/Audio/Effects/Cargo/buzz_sigh.ogg");

    [DataField("soundConfirm")]
    public SoundSpecifier ConfirmSound =
        new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg");

    /// <summary>
    /// Access levels to be added to the owner's ID card.
    /// </summary>
    [DataField]
    public List<ProtoId<AccessLevelPrototype>> NewAccessLevels = new();

    /// <summary>
    /// A tax rate that is imposed on the owner when a shuttle is sold. The tax is credited to
    /// the station's bank account.
    /// Expressed as a percentage: 0.3 means the owner loses 30% of the shuttle's value.
    /// </summary>
    [DataField]
    public float SalesTax = 0;

    /// <summary>
    /// If non-empty, specifies the new job title that should be given to the owner of the ship.
    /// </summary>
    [DataField]
    public string? NewJobTitle = null;

    /// <summary>
    /// The cost of making a new id card.
    /// This may be zero with different access levels, ie. if the SR uses the console.
    /// </summary>
    [DataField]
    public double TransactionPrice { get; set; } = 10000;
}
