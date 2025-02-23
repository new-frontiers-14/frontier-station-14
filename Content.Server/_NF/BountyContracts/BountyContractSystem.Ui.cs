using System.Linq;
using Content.Server._NF.SectorServices;
using Content.Server.StationRecords;
using Content.Shared._NF.BountyContracts;
using Content.Shared.Access.Components;
using Content.Shared.CartridgeLoader;
using Content.Shared.PDA;
using Content.Shared.StationRecords;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.BountyContracts;

public sealed partial class BountyContractSystem
{
    [Dependency] SectorServiceSystem _sectorService = default!;
    private void InitializeUi()
    {
        SubscribeLocalEvent<BountyContractsCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<BountyContractsCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
    }

    /// <summary>
    ///     Show create contract menu on ui cartridge.
    /// </summary>
    private void CartridgeOpenCreateUi(Entity<BountyContractsCartridgeComponent> cartridge, EntityUid loaderUid, ProtoId<BountyContractCollectionPrototype> collection)
    {
        var state = GetCreateState(cartridge, collection);
        _cartridgeLoader.UpdateCartridgeUiState(loaderUid, state);
    }

    /// <summary>
    ///     Show list all contracts menu on ui cartridge.
    /// </summary>
    private void CartridgeOpenListUi(Entity<BountyContractsCartridgeComponent> cartridge, EntityUid loaderUid, ProtoId<BountyContractCollectionPrototype>? collection = null)
    {
        var state = GetListState(cartridge, loaderUid, collection);

        if (state == null)
            return;

        _cartridgeLoader.UpdateCartridgeUiState(loaderUid, state);
    }

    private void CartridgeRefreshListUi(Entity<BountyContractsCartridgeComponent> cartridge, EntityUid loaderUid, ProtoId<BountyContractCollectionPrototype>? collection = null)
    {
        // this will technically refresh it
        // by sending list state again
        CartridgeOpenListUi(cartridge, loaderUid, collection);
    }

    private BountyContractListUiState? GetListState(Entity<BountyContractsCartridgeComponent> cartridge, EntityUid loaderUid, ProtoId<BountyContractCollectionPrototype>? collection = null)
    {
        // Set the cartridge's collection if requested.
        if (collection != null)
            cartridge.Comp.Collection = collection;

        var contracts = GetPermittedContracts(cartridge, loaderUid, out var newCollection).ToList();
        if (newCollection == null)
            return null;

        var isAllowedCreate = HasWriteAccess(loaderUid, newCollection.Value);
        var isAllowedRemove = HasDeleteAccess(loaderUid, newCollection.Value);

        if (cartridge.Comp.Collection != newCollection)
            cartridge.Comp.Collection = newCollection;

        return new BountyContractListUiState(newCollection.Value, GetReadableCollections(loaderUid), contracts, isAllowedCreate, isAllowedRemove, GetNetEntity(loaderUid));
    }

    private BountyContractCreateUiState GetCreateState(Entity<BountyContractsCartridgeComponent> cartridge, ProtoId<BountyContractCollectionPrototype> collection)
    {
        var bountyTargets = new HashSet<BountyContractTargetInfo>();
        var vessels = new HashSet<string>();

        // TODO: This will show all Stations, not only NT stations
        // TODO: Register all NT characters in some cache component on main station?
        var allStations = EntityQueryEnumerator<StationRecordsComponent, MetaDataComponent>();
        while (allStations.MoveNext(out var uid, out _, out var meta))
        {
            // get station IC name - its vessel name
            var name = meta.EntityName;
            vessels.Add(name);

            // get all characters registered on this station
            var icRecords = _records.GetRecordsOfType<GeneralStationRecord>(uid);
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

        return new BountyContractCreateUiState(collection, bountyTargets.ToList(), vessels.ToList());
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

    private void OnUiMessage(EntityUid uid, BountyContractsCartridgeComponent component, CartridgeMessageEvent args)
    {
        if (args is BountyContractCommandMessageEvent command)
            OnCommandMessage((uid, component), ref command);
        else if (args is BountyContractTryRemoveMessageEvent remove)
            OnTryRemoveMessage((uid, component), ref remove);
        else if (args is BountyContractTryCreateMessageEvent create)
            OnTryCreateMessage((uid, component), ref create);
    }

    private void OnCommandMessage(Entity<BountyContractsCartridgeComponent> cartridge, ref BountyContractCommandMessageEvent args)
    {
        switch (args.Command)
        {
            case BountyContractCommand.OpenCreateUi:
                CartridgeOpenCreateUi(cartridge, GetEntity(args.LoaderUid), args.Collection);
                break;
            case BountyContractCommand.CloseCreateUi:
                CartridgeOpenListUi(cartridge, GetEntity(args.LoaderUid), args.Collection);
                break;
            case BountyContractCommand.RefreshList:
                CartridgeRefreshListUi(cartridge, GetEntity(args.LoaderUid), args.Collection);
                break;
            default:
                return; //TODO: print to log?
        }
    }

    private void OnTryRemoveMessage(Entity<BountyContractsCartridgeComponent> cartridge, ref BountyContractTryRemoveMessageEvent args)
    {
        var entityUid = GetEntity(args.LoaderUid);

        var data = GetContracts();
        if (data == null || data.Contracts == null)
            return;

        // TODO: separate out "is this the author of this message" and "does this user have general delete permissions"
        // if (!HasDeleteAccess(entityUid, null, args.ContractId))
        //     return;

        // TODO: move this out of the UI.
        // Find the given collection this belongs to.
        ProtoId<BountyContractCollectionPrototype>? collectionId = null;
        foreach (var (collectionKey, collectionValue) in data.Contracts)
        {
            if (collectionValue.ContainsKey(args.ContractId))
            {
                collectionId = collectionKey;
                break;
            }
        }

        if (collectionId == null)
            return;

        var contract = data.Contracts[collectionId.Value][args.ContractId];

        // Check the delete access for the user on this collection.
        if (!HasDeleteAccess(entityUid, collectionId.Value, data) || !(contract.AuthorUid == entityUid))
            return;

        data.Contracts[collectionId.Value].Remove(args.ContractId);
        CartridgeRefreshListUi(cartridge, entityUid);
    }

    private void OnTryCreateMessage(Entity<BountyContractsCartridgeComponent> cartridge, ref BountyContractTryCreateMessageEvent args)
    {
        if (!HasWriteAccess(args.Actor, args.Contract.Collection))
            return;

        var loader = GetEntity(args.LoaderUid);

        var c = args.Contract;
        var author = GetContractAuthor(loader);
        CreateBountyContract(c.Collection, c.Category, c.Name, c.Reward, loader, c.Description, c.Vessel, c.DNA, author);

        CartridgeOpenListUi(cartridge, loader);
    }
}
