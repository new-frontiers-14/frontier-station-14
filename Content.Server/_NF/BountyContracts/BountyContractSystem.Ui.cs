using System.Linq;
using Content.Server._NF.SectorServices;
using Content.Server.StationRecords;
using Content.Shared._NF.BountyContracts;
using Content.Shared.Access.Components;
using Content.Shared.CartridgeLoader;
using Content.Shared.PDA;
using Content.Shared.StationRecords;

namespace Content.Server._NF.BountyContracts;

public sealed partial class BountyContractSystem
{
    [Dependency] SectorServiceSystem _sectorService = default!;
    private void InitializeUi()
    {
        SubscribeLocalEvent<BountyContractsCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<BountyContractsCartridgeComponent, BountyContractCommandMessageEvent>(OnCommandMessage);
        SubscribeLocalEvent<BountyContractsCartridgeComponent, BountyContractTryRemoveMessageEvent>(OnTryRemoveMessage);
        SubscribeLocalEvent<BountyContractsCartridgeComponent, BountyContractTryCreateMessageEvent>(OnTryCreateMessage);
    }

    /// <summary>
    ///     Show create contract menu on ui cartridge.
    /// </summary>
    private void CartridgeOpenCreateUi(EntityUid loaderUid)
    {
        var state = GetCreateState();
        _cartridgeLoader.UpdateCartridgeUiState(loaderUid, state);
    }

    /// <summary>
    ///     Show list all contracts menu on ui cartridge.
    /// </summary>
    private void CartridgeOpenListUi(Entity<BountyContractsCartridgeComponent> cartridge, EntityUid loaderUid)
    {
        var state = GetListState(cartridge, loaderUid);
        _cartridgeLoader.UpdateCartridgeUiState(loaderUid, state);
    }

    private void CartridgeRefreshListUi(Entity<BountyContractsCartridgeComponent> cartridge, EntityUid loaderUid)
    {
        // this will technically refresh it
        // by sending list state again
        CartridgeOpenListUi(cartridge, loaderUid);
    }

    private BountyContractListUiState GetListState(Entity<BountyContractsCartridgeComponent> cartridge, EntityUid loaderUid)
    {
        var contracts = GetPermittedContracts(cartridge, loaderUid, out var newCollection).ToList();
        var isAllowedCreate = false;
        var isAllowedRemove = false;
        if (newCollection != null)
        {
            isAllowedCreate = HasWriteAccess(loaderUid, newCollection.Value);
            isAllowedRemove = HasDeleteAccess(loaderUid, newCollection.Value);
        }
        if (cartridge.Comp.Collection != newCollection)
        {
            cartridge.Comp.Collection = newCollection;
            Dirty(cartridge);
        }

        return new BountyContractListUiState(newCollection, contracts, isAllowedCreate, isAllowedRemove);
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
        CartridgeOpenListUi((uid, component), args.Loader);
    }

    private void OnCommandMessage(Entity<BountyContractsCartridgeComponent> cartridge, ref BountyContractCommandMessageEvent args)
    {
        switch (args.Command)
        {
            case BountyContractCommand.OpenCreateUi:
                CartridgeOpenCreateUi(GetEntity(args.LoaderUid));
                break;
            case BountyContractCommand.CloseCreateUi:
                CartridgeOpenListUi(cartridge, GetEntity(args.LoaderUid));
                break;
            case BountyContractCommand.RefreshList:
                CartridgeRefreshListUi(cartridge, GetEntity(args.LoaderUid));
                break;
            default:
                return; //TODO: print to log?
        }
    }

    private void OnTryRemoveMessage(Entity<BountyContractsCartridgeComponent> cartridge, ref BountyContractTryRemoveMessageEvent args)
    {
        var entityUid = GetEntity(args.LoaderUid);

        // TODO: separate out "is this the author of this message" and "does this user have general delete permissions"
        // if (!HasDeleteAccess(entityUid, null, args.ContractId))
        //     return;

        RemoveBountyContract(args.ContractId);
        CartridgeRefreshListUi(cartridge, entityUid);
    }

    private void OnTryCreateMessage(EntityUid uid, BountyContractsCartridgeComponent component, BountyContractTryCreateMessageEvent args)
    {
        if (!HasWriteAccess(args.Actor, args.Contract.Collection))
            return;

        var loader = GetEntity(args.LoaderUid);

        var c = args.Contract;
        var author = GetContractAuthor(loader);
        CreateBountyContract(c.Collection, c.Category, c.Name, c.Reward, c.Description, c.Vessel, c.DNA, author);

        CartridgeOpenListUi((uid, component), loader);
    }
}
