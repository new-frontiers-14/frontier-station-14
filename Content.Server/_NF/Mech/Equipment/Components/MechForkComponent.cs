using System.Numerics;
using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server._NF.Mech.Equipment.Components;

/// <summary>
/// A piece of mech equipment that grabs entities and stores them
/// inside of a container so large objects can be moved.
/// </summary>
[RegisterComponent]
public sealed partial class MechForkComponent : Component
{
    /// <summary>
    /// The change in energy after each grab.
    /// </summary>
    [DataField]
    public float GrabEnergyDelta = -30;

    /// <summary>
    /// How long does it take to grab something?
    /// </summary>
    [DataField]
    public float GrabDelay = 2.5f;

    /// <summary>
    /// The offset from the mech when an item is dropped.
    /// This is here for things like lockers and vendors
    /// </summary>
    [DataField]
    public Vector2 DepositOffset = new(0, -1);

    /// <summary>
    /// The maximum amount of items that can be fit in this grabber
    /// </summary>
    [DataField]
    public int MaxContents = 5;

    /// <summary>
    /// The sound played when a mech is grabbing something
    /// </summary>
    [DataField]
    public SoundSpecifier GrabSound = new SoundPathSpecifier("/Audio/Mecha/sound_mecha_hydraulic.ogg");

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? AudioStream;

    [ViewVariables(VVAccess.ReadWrite)]
    public Container ItemContainer = default!;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public DoAfterId? DoAfter;

    /// <summary>
    /// A whitelist of things this fork can pick up.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// If true, interactions with storage will insert an object onto them.
    /// </summary>
    [DataField]
    public bool Inserting = true;

    [DataField]
    public EntProtoId ToggleAction = "ActionMechForkToggleInsert";

    [DataField]
    public EntityUid? ToggleActionEntity;
}
