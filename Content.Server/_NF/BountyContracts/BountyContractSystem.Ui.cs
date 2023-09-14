using System.Linq;
using Content.Server.StationRecords;
using Content.Shared._NF.BountyContracts;
using Content.Shared.Access.Components;
using Content.Shared.CartridgeLoader;
using Content.Shared.PDA;
using Content.Shared.StationRecords;

namespace Content.Server._NF.BountyContracts;

public sealed partial class BountyContractSystem
{
    [Dependency] private readonly EntityManager _entManager = default!;
    private void InitializeUi()
    {
        SubscribeLocalEvent<BountyContractsCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<CartridgeLoaderComponent, BountyContractOpenCreateUiMsg>(OnOpenCreateUi);
        SubscribeLocalEvent<CartridgeLoaderComponent, BountyContractCloseCreateUiMsg>(OnCloseCreateUi);
        SubscribeLocalEvent<CartridgeLoaderComponent, BountyContractTryCreateMsg>(OnTryCreateContract);
        SubscribeLocalEvent<CartridgeLoaderComponent, BountyContractRefreshListUiMsg>(OnRefreshContracts);
        SubscribeLocalEvent<CartridgeLoaderComponent, BountyContractTryRemoveUiMsg>(OnRemoveContract);
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

    private void CartridgeRefreshListUi(EntityUid loaderUid)
    {
        // this will technically refresh it
        // by sending list state again
        CartridgeOpenListUi(loaderUid);
    }

    private BountyContractListUiState GetListState(EntityUid loaderUid)
    {
        var contracts = GetAllContracts().ToList();
        var isAllowedCreate = IsAllowedCreateBounties(loaderUid);
        var isAllowedRemove = IsAllowedDeleteBounties(loaderUid);

        return new BountyContractListUiState(contracts, isAllowedCreate, isAllowedRemove);
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

        return new BountyContractCreateUiState(bountyTargets.ToList(), vessels.ToList());
    }

    private bool IsAllowedCreateBounties(EntityUid loaderUid, CartridgeLoaderComponent? component = null)
    {
        if (!Resolve(loaderUid, ref component) || component.ActiveProgram == null)
            return false;

        return _accessReader.IsAllowed(loaderUid, component.ActiveProgram.Value);
    }

    private bool IsAllowedDeleteBounties(EntityUid loaderUid, CartridgeLoaderComponent? component = null)
    {
        return IsAllowedCreateBounties(loaderUid, component);
    }

    private string? GetContractAuthor(EntityUid loaderUid, PdaComponent? component = null)
    {
        if (!Resolve(loaderUid, ref component))
            return null;

        TryComp<IdCardComponent>(component.ContainedId, out var id);
        var name = id?.FullName ?? Loc.GetString("bounty-contracts-unknown-author-name");
        var job = id?.JobTitle ?? Loc.GetString("bounty-contracts-unknown-author-job");
        return Loc.GetString("bounty-contracts-author", ("name", name), ("job", job));
    }

    private void OnUiReady(EntityUid uid, BountyContractsCartridgeComponent component, CartridgeUiReadyEvent args)
    {
        CartridgeOpenListUi(args.Loader);
    }

    private void OnOpenCreateUi(EntityUid uid, CartridgeLoaderComponent component, BountyContractOpenCreateUiMsg args)
    {
        CartridgeOpenCreateUi(_entManager.GetEntity(args.Entity));
    }

    private void OnCloseCreateUi(EntityUid uid, CartridgeLoaderComponent component, BountyContractCloseCreateUiMsg args)
    {
        CartridgeOpenListUi(_entManager.GetEntity(args.Entity));
    }

    private void OnTryCreateContract(EntityUid uid, CartridgeLoaderComponent component, BountyContractTryCreateMsg args)
    {
        if (!IsAllowedCreateBounties(_entManager.GetEntity(args.Entity)))
            return;

        var c = args.Contract;
        var author = GetContractAuthor(_entManager.GetEntity(args.Entity));
        CreateBountyContract(c.Category, c.Name, c.Reward, c.Description, c.Vessel, c.DNA, author);

        CartridgeOpenListUi(_entManager.GetEntity(args.Entity));
    }

    private void OnRefreshContracts(EntityUid uid, CartridgeLoaderComponent component, BountyContractRefreshListUiMsg args)
    {
        CartridgeRefreshListUi(_entManager.GetEntity(args.Entity));
    }

    private void OnRemoveContract(EntityUid uid, CartridgeLoaderComponent component, BountyContractTryRemoveUiMsg args)
    {
        if (!IsAllowedDeleteBounties(_entManager.GetEntity(args.Entity)))
            return;

        RemoveBountyContract(args.ContractId);
        CartridgeRefreshListUi(_entManager.GetEntity(args.Entity));
    }
}
