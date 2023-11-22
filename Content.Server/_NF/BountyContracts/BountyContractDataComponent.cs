using Content.Shared._NF.BountyContracts;

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
    [DataField("lastId")]
    public uint LastId;

    /// <summary>
    ///     All open bounty contracts by their contract id.
    /// </summary>
    [DataField("contracts")]
    public Dictionary<uint, BountyContract> Contracts = new();
}
