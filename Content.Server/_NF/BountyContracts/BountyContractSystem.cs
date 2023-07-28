using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.CartridgeLoader;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking.Events;
using Content.Server.StationRecords.Systems;
using Content.Shared._NF.BountyContracts;
using Content.Shared.Access.Systems;
using Robust.Shared.Map;

namespace Content.Server._NF.BountyContracts;

/// <summary>
///     Used to control all bounty contracts placed by players.
/// </summary>
public sealed partial class BountyContractSystem : SharedBountyContractSystem
{
    private ISawmill _sawmill = default!;

    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoaderSystem = default!;
    [Dependency] private readonly StationRecordsSystem _records = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = Logger.GetSawmill("bounty.contracts");

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
        InitializeUi();
    }

    private void OnRoundStarting(RoundStartingEvent ev)
    {
        // TODO: move to in-game server like RD?

        // delete all existing data component
        // just in case someone added it on map or previous round ended weird
        var query = EntityQuery<BountyContractDataComponent>();
        foreach (var bnt in query)
        {
            RemCompDeferred(bnt.Owner, bnt);
        }

        // use nullspace entity to store all information about contracts
        var uid = Spawn(null, MapCoordinates.Nullspace);
        EnsureComp<BountyContractDataComponent>(uid);
    }

    private BountyContractDataComponent? GetContracts()
    {
        // we assume that there is only one bounty database for round
        // if it doesn't exist - game should work fine
        // but players wouldn't able to create/get contracts
        return EntityQuery<BountyContractDataComponent>().FirstOrDefault();
    }

    /// <summary>
    ///     Try to create a new bounty contract and put it in bounties list.
    /// </summary>
    /// <param name="category">Bounty contract category (bounty head, construction, etc.)</param>
    /// <param name="name">IC name for the contract bounty head. Can be players IC name or custom string.</param>
    /// <param name="reward">Cash reward for completing bounty. Can be zero.</param>
    /// <param name="description">IC description of players crimes, details, etc.</param>
    /// <param name="vessel">IC name of last known bounty vessel. Can be station/ship name or custom string.</param>
    /// <param name="dna">Optional DNA of the bounty head.</param>
    /// <param name="author">Optional bounty poster IC name.</param>
    /// <param name="postToRadio">Should radio message about contract be posted in general radio channel?</param>
    /// <returns>New bounty contract. Null if contract creation failed.</returns>
    public BountyContract? CreateBountyContract(BountyContractCategory category,
        string name, int reward,
        string? description = null, string? vessel = null,
        string? dna = null, string? author = null,
        bool postToRadio = true)
    {
        var data = GetContracts();
        if (data == null)
            return null;

        // create a new contract
        var contractId = data.LastId++;
        var contract = new BountyContract(contractId, category, name, reward,
            dna, vessel, description, author);

        // try to save it
        if (!data.Contracts.TryAdd(contractId, contract))
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

        return data.Contracts.TryGetValue(contractId, out contract);
    }

    /// <summary>
    ///     Try to get all bounty contracts available.
    /// </summary>
    public IEnumerable<BountyContract> GetAllContracts()
    {
        var data = GetContracts();
        if (data == null)
            return Enumerable.Empty<BountyContract>();

        return data.Contracts.Values;
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

        if (!data.Contracts.Remove(contractId))
        {
            _sawmill.Warning($"Failed to remove bounty contract with {contractId}!");
            return false;
        }

        return true;
    }
}
