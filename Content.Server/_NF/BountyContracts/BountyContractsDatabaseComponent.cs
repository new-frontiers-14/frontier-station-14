using Content.Shared.StationBounties;

namespace Content.Server._NF.BountyContracts;

/// <summary>
///     Store all bounty contracts information.
/// </summary>
[RegisterComponent]
public sealed class BountyContractsDatabaseComponent : Component
{
    [DataField("lastId")]
    public uint LastId;

    [DataField("contracts")]
    public Dictionary<uint, BountyContract> Contracts = new();
}
