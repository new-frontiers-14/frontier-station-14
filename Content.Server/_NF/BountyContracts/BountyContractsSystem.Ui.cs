using System.Linq;
using Content.Server.StationRecords;
using Content.Shared.CartridgeLoader;
using Content.Shared.StationBounties;
using Content.Shared.StationRecords;

namespace Content.Server._NF.BountyContracts;

public sealed partial class BountyContractsSystem
{
    private void InitializeUi()
    {
        SubscribeLocalEvent<BountyContractsComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<CartridgeLoaderComponent, BountyContractOpenCreateUiMsg>(OnOpenCreateUi);
        SubscribeLocalEvent<CartridgeLoaderComponent, BountyContractCloseCreateUiMsg>(OnCloseCreateUi);
        SubscribeLocalEvent<CartridgeLoaderComponent, BountyContractTryCreateMsg>(OnTryCreateContract);
    }

    /// <summary>
    ///     Show create contract menu on ui cartridge.
    /// </summary>
    private void CartridgeOpenCreateUi(EntityUid loaderUid)
    {
        var state = GetCreateState();
        _cartridgeLoaderSystem.UpdateCartridgeUiState(loaderUid, state);
    }

    /// <summary>
    ///     Show list all contracts menu on ui cartridge.
    /// </summary>
    private void CartridgeOpenListUi(EntityUid loaderUid)
    {
        var state = GetListState(loaderUid);
        _cartridgeLoaderSystem.UpdateCartridgeUiState(loaderUid, state);
    }

    private BountyContractListUiState GetListState(EntityUid loaderUid)
    {
        var contracts = GetAllContracts().ToList();
        var isAllowedCreate = IsAllowedCreateBounties(loaderUid);

        return new BountyContractListUiState(contracts, isAllowedCreate);
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

    private bool IsAllowedCreateBounties(EntityUid loaderUid, CartridgeLoaderComponent? component = null)
    {
        if (!Resolve(loaderUid, ref component) || component.ActiveProgram == null)
            return false;

        return _accessReader.IsAllowed(loaderUid, component.ActiveProgram.Value);
    }


    private void OnUiReady(EntityUid uid, BountyContractsComponent component, CartridgeUiReadyEvent args)
    {
        CartridgeOpenListUi(args.Loader);
    }

    private void OnOpenCreateUi(EntityUid uid, CartridgeLoaderComponent component, BountyContractOpenCreateUiMsg args)
    {
        CartridgeOpenCreateUi(args.Entity);
    }

    private void OnCloseCreateUi(EntityUid uid, CartridgeLoaderComponent component, BountyContractCloseCreateUiMsg args)
    {
        CartridgeOpenListUi(args.Entity);
    }

    private void OnTryCreateContract(EntityUid uid, CartridgeLoaderComponent component, BountyContractTryCreateMsg args)
    {
        CreateBountyContract(args.Contract);
        CartridgeOpenListUi(args.Entity);
    }
}
