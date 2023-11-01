﻿using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Silicons.Laws.Components;

/// <summary>
/// This is used for an entity that grants a special "obey" law when emagged.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSiliconLawSystem))]
public sealed partial class EmagSiliconLawComponent : Component
{
    /// <summary>
    /// The name of the person who emagged this law provider.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string? OwnerName;

    /// <summary>
    /// Does the panel need to be open to EMAG this law provider.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool RequireOpenPanel = true;

    /// <summary>
    /// The laws that the borg is given when emagged.
    /// Law 0 is prepended to this, so this can only include the static laws.
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<SiliconLawsetPrototype> EmagLaws = string.Empty;

    /// <summary>
    /// Lawset created from the prototype id and law 0.
    /// Cached when getting laws and only modified during an ion storm event.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SiliconLawset? Lawset;

    /// <summary>
    /// How long the borg is stunned when it's emagged. Setting to 0 will disable it.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan StunTime = TimeSpan.Zero;

    /// <summary>
    /// A role given to entities with this component when they are emagged.
    /// Mostly just for admin purposes.
    /// </summary>
    [DataField]
    public ProtoId<AntagPrototype>? AntagonistRole = "SubvertedSilicon";
}
