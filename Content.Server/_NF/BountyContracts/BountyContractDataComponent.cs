using System.Collections.Frozen;
using Content.Shared._NF.BountyContracts;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.BountyContracts;

/// <summary>
///     Store all bounty contracts information.
/// </summary>
[RegisterComponent]
[Access(typeof(BountyContractSystem))]
public sealed partial class BountyContractDataComponent : Component
{
    /// <summary>
    ///     Last registered contract id. Used to track contracts.
    /// </summary>
    [DataField]
    public uint LastId;

    /// <summary>
    ///     All open bounty contracts, grouped by collection, listed by their contract id.
    /// </summary>
    [DataField]
    public FrozenDictionary<ProtoId<BountyContractCollectionPrototype>, Dictionary<uint, BountyContract>>? Contracts = null;

    /// <summary>
    ///     A cached list of prototype IDs by their order
    /// </summary>
    [DataField]
    public List<ProtoId<BountyContractCollectionPrototype>> OrderedCollections = new();
}
