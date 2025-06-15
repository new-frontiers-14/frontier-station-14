using Robust.Shared.GameStates;
using Robust.Shared.Audio;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Prototypes;
using Content.Shared.Radio;
using Content.Shared.Access;
using Content.Shared._NF.Bank.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._NF.Shipyard.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedShipyardSystem)), AutoGenerateComponentPause]
public sealed partial class ShipyardConsoleComponent : Component
{
    public static string TargetIdCardSlotId = "ShipyardConsole-targetId";

    [DataField]
    public ItemSlot TargetIdSlot = new();

    [DataField]
    public SoundSpecifier ErrorSound =
        new SoundPathSpecifier("/Audio/Effects/Cargo/buzz_sigh.ogg");

    [DataField]
    public SoundSpecifier ConfirmSound =
        new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg");

    /// <summary>
    /// The comms channel that announces the ship purchase. The purchase is *always* announced
    /// on this channel.
    /// </summary>
    [DataField]
    public ProtoId<RadioChannelPrototype> ShipyardChannel = "Traffic";

    /// <summary>
    /// A second comms channel that announces the ship purchase, with some information redacted.
    /// Currently used for black market and syndicate shipyards to alert the NFSD.
    /// </summary>
    [DataField]
    public ProtoId<RadioChannelPrototype>? SecretShipyardChannel = null;

    /// <summary>
    /// If non-empty, specifies the new job title that should be given to the owner of the ship.
    /// </summary>
    [DataField]
    public LocId? NewJobTitle;

    /// <summary>
    /// Access levels to be added to the owner's ID card.
    /// </summary>
    [DataField]
    public List<ProtoId<AccessLevelPrototype>> NewAccessLevels = new();

    /// <summary>
    /// Indicates that the deeds that come from this console can be copied and transferred.
    /// </summary>
    [DataField]
    public bool CanTransferDeed = true;

    /// <summary>
    /// The accounts to receive payment, and the tax rate to apply for ship sales from this console.
    /// </summary>
    [DataField]
    public Dictionary<SectorBankAccount, float> TaxAccounts = new();

    /// <summary>
    /// If true, the base sale rate is ignored before calculating taxes.
    /// </summary>
    [DataField]
    public bool IgnoreBaseSaleRate;

    /// <summary>
    /// The time at which the console will be able to play the deny sound.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextDenySoundTime = TimeSpan.Zero;

    /// <summary>
    /// The minimum time between playing the deny sound.
    /// </summary>
    [DataField]
    public TimeSpan DenySoundDelay = TimeSpan.FromSeconds(2);
}
