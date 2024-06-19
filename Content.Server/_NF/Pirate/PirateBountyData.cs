using Robust.Shared.Serialization;
using Content.Shared._NF.Pirate.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._NF.Pirate;

/// <summary>
/// A data structure for storing currently available bounties.
/// </summary>
[DataDefinition, NetSerializable, Serializable]
public readonly partial record struct PirateBountyData
{
    /// <summary>
    /// A unique id used to identify the bounty
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// The prototype containing information about the bounty.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField(required: true)]
    public ProtoId<PirateBountyPrototype> Bounty { get; init; } = string.Empty;

    public PirateBountyData(PirateBountyPrototype bounty, int uniqueIdentifier)
    {
        Bounty = bounty.ID;
        Id = $"{bounty.IdPrefix}{uniqueIdentifier:D3}";
    }
}
