using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.Pirate.Prototypes;

/// <summary>
/// This is a prototype for a pirate bounty, a set of items
/// that must be sold together in a labeled container in order
/// to receive a reward in doubloons.
/// </summary>
[Prototype, Serializable, NetSerializable]
public sealed partial class PirateBountyPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The monetary reward for completing the bounty
    /// </summary>
    [DataField(required: true)]
    public int Reward;

    /// <summary>
    /// A description for flava purposes.  If empty, will fallback to a default option.
    /// </summary>
    [DataField]
    public LocId Description = string.Empty;

    /// <summary>
    /// The entries that must be satisfied for the cargo bounty to be complete.
    /// </summary>
    [DataField(required: true)]
    public List<PirateBountyItemEntry> Entries = new();

    /// <summary>
    /// A prefix appended to the beginning of a bounty's ID.
    /// </summary>
    [DataField]
    public string IdPrefix = "NT";
}

[DataDefinition, Serializable, NetSerializable]
public readonly partial record struct PirateBountyItemEntry()
{
    /// <summary>
    /// A whitelist for determining what items satisfy the entry by tag, component, etc.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist Whitelist { get; init; } = default!;

    /// <summary>
    /// A whitelist for determining what items satisfy the entry by entity prototype ID
    /// </summary>
    [DataField(required: true)]
    public EntProtoIdWhitelist IdWhitelist { get; init; } = default!;

    /// <summary>
    /// How much of the item must be present to satisfy the entry
    /// </summary>
    [DataField]
    public int Amount { get; init; } = 1;

    /// <summary>
    /// A player-facing name for the item.
    /// </summary>
    [DataField]
    public LocId Name { get; init; } = string.Empty;
}
