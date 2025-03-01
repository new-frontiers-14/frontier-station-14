using Content.Shared.Access;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.BountyContracts;

/// <summary>
///     Describes a collection of bounty contracts, including who can read or post to it.
/// </summary>
[Prototype]
public sealed partial class BountyContractCollectionPrototype : IPrototype
{
    /// <inheritdoc/>
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Localized name to describe the bounty contract type.
    /// </summary>
    [DataField]
    public LocId Name { get; private set; } = default!;

    /// <summary>
    /// The order to show in the tabbed bounty collections. Lower appears first.
    /// </summary>
    [DataField]
    public int Order { get; private set; } = 0;

    /// <summary>
    /// The type of notification to send off when bounty contract.
    /// </summary>
    [DataField]
    public BountyContractNotificationType NotificationType { get; private set; } = BountyContractNotificationType.None;

    /// <summary>
    /// Localized name to describe the bounty contract type.
    /// </summary>
    [DataField]
    public List<BountyContractCategory> Categories { get; private set; } = new();

    /// <summary>
    /// Access levels required to post to this contract type.
    /// </summary>
    [DataField]
    public List<ProtoId<AccessLevelPrototype>> WriteAccess { get; private set; } = new();

    /// <summary>
    /// Access groups required to post to this contract type.
    /// </summary>
    [DataField]
    public List<ProtoId<AccessGroupPrototype>> WriteGroups { get; private set; } = new();

    /// <summary>
    /// Access levels required to read this contract type.
    /// </summary>
    [DataField]
    public List<ProtoId<AccessLevelPrototype>> ReadAccess { get; private set; } = new();

    /// <summary>
    /// Access groups required to read this contract type.
    /// </summary>
    [DataField]
    public List<ProtoId<AccessGroupPrototype>> ReadGroups { get; private set; } = new();

    /// <summary>
    /// Access levels required to delete an arbitrary bounty.
    /// </summary>
    [DataField]
    public List<ProtoId<AccessLevelPrototype>> DeleteAccess { get; private set; } = new();

    /// <summary>
    /// Access groups required to delete an arbitrary bounty.
    /// </summary>
    [DataField]
    public List<ProtoId<AccessGroupPrototype>> DeleteGroups { get; private set; } = new();
}

[Serializable, NetSerializable]
public enum BountyContractNotificationType : byte
{
    None = 0,
    PDA = 1,
    Radio = 2,
}
