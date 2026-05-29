using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Audio; // Frontier

namespace Content.Shared._DV.Silicons.Borgs;

/// <summary>
/// Adds an id chip slot for a borg which will control its access.
/// Enables an id chip's access when inserted to an id chip slot.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(IdChipSlotSystem))]
public sealed partial class IdChipSlotComponent : Component
{
    /// <summary>
    /// The container ID to use for the slot.
    /// </summary>
    [DataField]
    public string ContainerId = "borg_id_chip";

    /// <summary>
    /// The container that can hold an id chip.
    /// </summary>
    [ViewVariables]
    public ContainerSlot Container = default!;

    /// <summary>
    /// The id chip installed, if any.
    /// </summary>
    [ViewVariables]
    public EntityUid? Chip => Container.ContainedEntity;

    /// <summary>
    /// The whitelist an id chip must match to be inserted.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist Whitelist = new();

    // Frontier begin
    /// <summary>
    /// Audio to be played when ejecting a borg chip from the chassis
    /// </summary>
    [DataField]
    public SoundSpecifier SwipeSound = new SoundPathSpecifier("/Audio/Machines/id_swipe.ogg");

    /// <summary>
    /// Audio to be played when inserting a borg chip into the chassis
    /// </summary>
    [DataField]
    public SoundSpecifier InsertSound = new SoundPathSpecifier("/Audio/Machines/id_insert.ogg");
    // Frontier end
}

/// <summary>
/// BUI message to eject a borg's id chip.
/// </summary>
[Serializable, NetSerializable]
public sealed class BorgEjectIdChipMessage : BoundUserInterfaceMessage;
