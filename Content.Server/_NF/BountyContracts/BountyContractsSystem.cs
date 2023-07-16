using System.Linq;
using Content.Server.CartridgeLoader;
using Content.Server.CrewManifest;
using Content.Server.Station.Systems;
using Content.Server.StationRecords;
using Content.Server.StationRecords.Systems;
using Content.Shared.CartridgeLoader;
using Content.Shared.PDA;
using Content.Shared.StationBounties;
using Content.Shared.StationRecords;

namespace Content.Server._NF.BountyContracts;

public sealed class BountyContractsSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoaderSystem = default!;
    [Dependency] private readonly StationSystem _stations = default!;
    [Dependency] private readonly StationRecordsSystem _records = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BountyContractsComponent, CartridgeUiReadyEvent>(OnUiReady);
    }

    private void OnUiReady(EntityUid uid, BountyContractsComponent component, CartridgeUiReadyEvent args)
    {
        var state = GetListState();
        _cartridgeLoaderSystem.UpdateCartridgeUiState(args.Loader, state);
    }

    private BountyContractListUiState GetListState()
    {
        var contracts = new List<BountyContract>();
        return new BountyContractListUiState(contracts);
    }

    private BountyContractCreateUiState GetCreateState()
    {
        var bountyTargets = new HashSet<BountyContractTargetInfo>();
        var vessels = new HashSet<string>();

        // TODO: This will show all Stations, not only NT stations
        // TODO: Register all NT characters in some cache component on main station?
        var allStations = EntityQuery<StationRecordsComponent, MetaDataComponent>();
        foreach (var (records, meta) in allStations)
        {
            // get station IC name - it's vessel name
            var name = meta.EntityName;
            vessels.Add(name);

            // get all characters registered on this station
            var icRecords = _records.GetRecordsOfType<GeneralStationRecord>(records.Owner);
            foreach (var (_, icRecord) in icRecords)
            {
                var target = new BountyContractTargetInfo
                {
                    Name = icRecord.Name,
                    DNA = icRecord.DNA
                };

                // hashset will check if record is unique based on DNA field
                bountyTargets.Add(target);
            }
        }

        return new BountyContractCreateUiState(
            bountyTargets.ToList(), vessels.ToList());
    }
}
