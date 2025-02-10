using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server._NF.SectorServices;
using Content.Server.CartridgeLoader;
using Content.Server.Chat.Systems;
using Content.Server.StationRecords.Systems;
using Content.Shared._NF.BountyContracts;
using Content.Shared.Access.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.BountyContracts;

/// <summary>
///     Used to control all bounty contracts placed by players.
/// </summary>
public sealed partial class BountyContractSystem : SharedBountyContractSystem
{
    private ISawmill _sawmill = default!;

    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoaderSystem = default!;
    [Dependency] private readonly SectorServiceSystem _sectorServices = default!;
    [Dependency] private readonly StationRecordsSystem _records = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly PrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = Logger.GetSawmill("bounty.contracts");

        SubscribeLocalEvent<BountyContractDataComponent, ComponentInit>(ContractInit);
        InitializeUi();
    }

    private void ContractInit(Entity<BountyContractDataComponent> ent, ref ComponentInit ev)
    {
        Dictionary<ProtoId<BountyContractCollectionPrototype>, Dictionary<uint, BountyContract>> contracts = new();
        foreach (var proto in _proto.EnumeratePrototypes<BountyContractCollectionPrototype>())
        {
            contracts[proto.ID] = new();
        }
        ent.Comp.Contracts = contracts.ToFrozenDictionary();
    }

    private BountyContractDataComponent? GetContracts()
    {
        TryComp(_sectorServices.GetServiceEntity(), out BountyContractDataComponent? bountyContracts);
        return bountyContracts;
    }

    /// <summary>
    ///     Try to create a new bounty contract and put it in bounties list.
    /// </summary>
    /// <param name="collection">Bounty contract collection (command, public, etc.)</param>
    /// <param name="category">Bounty contract category (bounty head, construction, etc.)</param>
    /// <param name="name">IC name for the contract bounty head. Can be players IC name or custom string.</param>
    /// <param name="reward">Cash reward for completing bounty. Can be zero.</param>
    /// <param name="description">IC description of players crimes, details, etc.</param>
    /// <param name="vessel">IC name of last known bounty vessel. Can be station/ship name or custom string.</param>
    /// <param name="dna">Optional DNA of the bounty head.</param>
    /// <param name="author">Optional bounty poster IC name.</param>
    /// <param name="postToRadio">Should radio message about contract be posted in general radio channel?</param>
    /// <returns>New bounty contract. Null if contract creation failed.</returns>
    public BountyContract? CreateBountyContract(ProtoId<BountyContractCollectionPrototype> collection,
        BountyContractCategory category,
        string name, int reward,
        string? description = null, string? vessel = null,
        string? dna = null, string? author = null,
        bool postToRadio = true)
    {
        var data = GetContracts();
        if (data == null)
            return null;

        if (!data.Contracts.TryGetValue(collection, out var contracts))
            return null;

        // create a new contract
        var contractId = data.LastId++;
        var contract = new BountyContract(contractId, category, name, reward,
            dna, vessel, description, author);

        // try to save it
        if (!contracts.TryAdd(contractId, contract))
        {
            _sawmill.Error($"Failed to create bounty contract with {contractId}! LastId: {data.LastId}.");
            return null;
        }

        if (postToRadio)
        {
            // TODO: move this to radio in future?
            var sender = Loc.GetString("bounty-contracts-radio-name");
            var target = !string.IsNullOrEmpty(contract.Vessel)
                ? $"{contract.Name} ({contract.Vessel})"
                : contract.Name;
            var msg = Loc.GetString("bounty-contracts-radio-create",
                ("target", target), ("reward", contract.Reward));
            var color = Color.FromHex("#D7D7BE");
            _chat.DispatchGlobalAnnouncement(sender, msg, false, colorOverride: color);
        }

        return contract;
    }

    /// <summary>
    ///     Try to get a bounty contract by its id.
    /// </summary>
    public bool TryGetContract(uint contractId, [NotNullWhen(true)] out BountyContract? contract)
    {
        contract = null;
        var data = GetContracts();
        if (data == null)
            return false;

        // Linear w.r.t. collections, but should be a small collection
        foreach (var collection in data.Contracts.Values)
        {
            if (collection.TryGetValue(contractId, out contract))
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Try to get all bounty contracts available within a particular collection.
    /// </summary>
    public IEnumerable<BountyContract> GetAllContracts(ProtoId<BountyContractCollectionPrototype> collection)
    {
        var data = GetContracts();
        if (data == null)
            return Enumerable.Empty<BountyContract>();

        if (!data.Contracts.TryGetValue(collection, out var contracts))
            return Enumerable.Empty<BountyContract>();

        return contracts.Values;
    }

    /// <summary>
    ///     Try to remove bounty contract by its id.
    /// </summary>
    /// <returns>True if contract was found and removed.</returns>
    public bool RemoveBountyContract(uint contractId)
    {
        var data = GetContracts();
        if (data == null)
            return false;

        foreach (var collection in data.Contracts.Values)
        {
            if (collection.Remove(contractId))
                return true;
        }

        _sawmill.Warning($"Failed to remove bounty contract with {contractId}!");
        return true;
    }
}
