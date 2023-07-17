using System.Linq;
using Content.Server.CartridgeLoader;
using Content.Server.GameTicking.Events;
using Content.Server.StationRecords.Systems;
using Content.Shared.StationBounties;
using Robust.Shared.Map;

namespace Content.Server._NF.BountyContracts;

public sealed partial class BountyContractsSystem : EntitySystem
{
    private ISawmill _sawmill = default!;

    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoaderSystem = default!;
    [Dependency] private readonly StationRecordsSystem _records = default!;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = Logger.GetSawmill("bounty.contracts");
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);

        InitializeUi();
    }

    private void OnRoundStarting(RoundStartingEvent ev)
    {
        var uid = Spawn(null, MapCoordinates.Nullspace);
        AddComp<BountyContractsDatabaseComponent>(uid);
    }

    private BountyContractsDatabaseComponent? GetContracts()
    {
        // we assume that there is only one bounty database
        // if it doesn't exist - game should work fine
        // but players wouldn't able to create/get contracts
        return EntityQuery<BountyContractsDatabaseComponent>().FirstOrDefault();
    }

    public BountyContract? CreateBountyContract(BountyContractRequest request)
    {
        var data = GetContracts();
        if (data == null)
            return null;

        // create a new contract
        var contractId = data.LastId++;
        var contract = new BountyContract
        {
            ContractId = contractId,
            Name = request.Name,
            DNA = request.DNA,
            Vesel = request.Vesel,
            Reward = request.Reward,
            Description = request.Description
        };

        // try to save it
        if (!data.Contracts.TryAdd(contractId, contract))
        {
            _sawmill.Error($"Failed to create bounty contract with {contractId}!");
            return null;
        }

        return contract;
    }

    public IEnumerable<BountyContract> GetAllContracts()
    {
        var data = GetContracts();
        if (data == null)
            return Enumerable.Empty<BountyContract>();

        return data.Contracts.Values;
    }

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
