using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server._NF.Access;
using Content.Server.Administration.Logs;
using Content.Server.CartridgeLoader;
using Content.Server.Chat.Systems;
using Content.Server.StationRecords.Systems;
using Content.Shared._NF.Bank;
using Content.Shared._NF.BountyContracts;
using Content.Shared.Access.Systems;
using Content.Shared.CartridgeLoader;
using Content.Shared.Database;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.BountyContracts;

/// <summary>
///     Used to control all bounty contracts placed by players.
/// </summary>
public sealed partial class BountyContractSystem : SharedBountyContractSystem
{
    private ISawmill _sawmill = default!;

    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoader = default!;
    [Dependency] private readonly StationRecordsSystem _records = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly NFAccessSystemUtilities _accessUtils = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = Logger.GetSawmill("bounty.contracts");

        SubscribeLocalEvent<BountyContractDataComponent, ComponentInit>(ContractInit);
        InitializeUi();
    }

    private void ContractInit(Entity<BountyContractDataComponent> ent, ref ComponentInit ev)
    {
        SortedList<int, ProtoId<BountyContractCollectionPrototype>> orderedCollections = new();
        Dictionary<ProtoId<BountyContractCollectionPrototype>, Dictionary<uint, BountyContract>> contracts = new();
        foreach (var proto in _proto.EnumeratePrototypes<BountyContractCollectionPrototype>())
        {
            contracts[proto.ID] = new();
            orderedCollections[proto.Order] = proto.ID;
        }
        ent.Comp.Contracts = contracts.ToFrozenDictionary();
        ent.Comp.OrderedCollections = orderedCollections.Values.ToList();
    }

    private BountyContractDataComponent? GetContracts()
    {
        TryComp(_sectorService.GetServiceEntity(), out BountyContractDataComponent? bountyContracts);
        return bountyContracts;
    }

    // Returns a list of all readable collections that a user can see.
    private List<ProtoId<BountyContractCollectionPrototype>> GetReadableCollections(EntityUid user, BountyContractDataComponent? bounties = null)
    {
        var returnList = new List<ProtoId<BountyContractCollectionPrototype>>();
        if (bounties == null)
        {
            bounties = GetContracts();
            // Nothing to read from, no read access
            if (bounties == null)
                return returnList;
        }

        if (bounties.Contracts == null)
            return returnList;

        var accessTags = _accessReader.FindAccessTags(user);
        foreach (var collection in bounties.OrderedCollections)
        {
            if (!_proto.TryIndex(collection, out var collectionProto))
                continue;

            if (_accessUtils.IsAllowed(accessTags, collectionProto.ReadAccess, collectionProto.ReadGroups))
                returnList.Add(collection);
        }
        return returnList;
    }

    private bool HasReadAccess(EntityUid user, ProtoId<BountyContractCollectionPrototype> collection, BountyContractDataComponent? bounties = null)
    {
        if (bounties == null)
        {
            bounties = GetContracts();
            // Nothing to read from, no read access
            if (bounties == null)
                return false;
        }

        if (!_proto.TryIndex(collection, out var collectionProto))
            return false;

        return _accessUtils.IsAllowed(_accessReader.FindAccessTags(user), collectionProto.ReadAccess, collectionProto.ReadGroups);
    }

    private bool HasWriteAccess(EntityUid user, ProtoId<BountyContractCollectionPrototype> collection, BountyContractDataComponent? bounties = null)
    {
        if (bounties == null)
        {
            bounties = GetContracts();
            // Nothing to write to, no write access
            if (bounties == null)
                return false;
        }

        if (!_proto.TryIndex(collection, out var collectionProto))
            return false;

        return _accessUtils.IsAllowed(_accessReader.FindAccessTags(user), collectionProto.WriteAccess, collectionProto.WriteGroups);
    }

    private bool HasDeleteAccess(EntityUid user, ProtoId<BountyContractCollectionPrototype> collection, BountyContractDataComponent? bounties = null)
    {
        if (bounties == null)
        {
            bounties = GetContracts();
            // Nothing to delete from, no write access
            if (bounties == null)
                return false;
        }

        if (!_proto.TryIndex(collection, out var collectionProto))
            return false;

        return _accessUtils.IsAllowed(_accessReader.FindAccessTags(user), collectionProto.DeleteAccess, collectionProto.DeleteGroups);
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
    /// <param name="authorUid">Uid of the cartridge loader that created the bounty</param>
    /// <param name="pdaAlert">Should PDAs send a localized alert?</param>
    /// <param name="actor">The entity posting the bounty.</param>
    /// <returns>New bounty contract. Null if contract creation failed.</returns>
    public BountyContract? TryCreateBountyContract(ProtoId<BountyContractCollectionPrototype> collection,
        BountyContractCategory category,
        string name,
        int reward,
        EntityUid authorUid,
        EntityUid actor,
        string? description = null,
        string? vessel = null,
        string? dna = null,
        string? author = null)
    {
        var data = GetContracts();
        if (data == null
            || data.Contracts == null
            || !data.Contracts.TryGetValue(collection, out var contracts)
            || !HasWriteAccess(authorUid, collection))
        {
            return null;
        }

        if (name.Length > MaxNameLength)
            name = name.Substring(0, MaxNameLength);
        if (vessel != null && vessel.Length > MaxVesselLength)
            vessel = vessel.Substring(0, MaxVesselLength);
        if (description != null && description.Length > MaxDescriptionLength)
            description = description.Substring(0, MaxDescriptionLength);

        // create a new contract
        var contractId = data.LastId++;
        var contract = new BountyContract(contractId, category, name, reward, GetNetEntity(authorUid),
            dna, vessel, description, author);

        // try to save it
        if (!contracts.TryAdd(contractId, contract))
        {
            _sawmill.Error($"Failed to create bounty contract with {contractId}! LastId: {data.LastId}.");
            return null;
        }

        var notificationType = BountyContractNotificationType.None;
        if (_proto.TryIndex(collection, out var bountyCollection))
            notificationType = bountyCollection.NotificationType;

        LocId announcement = "bounty-contracts-announcement-generic-create";
        if (CategoriesMeta.TryGetValue(category, out var categoryMeta) && categoryMeta.Announcement != null)
            announcement = categoryMeta.Announcement.Value;

        // Generate a notification
        if (notificationType == BountyContractNotificationType.PDA)
        {
            var sender = Loc.GetString("bounty-contracts-announcement-pda-name");
            var target = !string.IsNullOrEmpty(contract.Vessel) && contract.Vessel != Loc.GetString("bounty-contracts-ui-create-vessel-unknown")
                ? $"{contract.Name} ({contract.Vessel})"
                : contract.Name;
            var msg = Loc.GetString(announcement,
                ("target", target), ("reward", BankSystemExtensions.ToSpesoString(contract.Reward)));

            var pdaList = EntityQueryEnumerator<CartridgeLoaderComponent>();
            while (pdaList.MoveNext(out var loaderUid, out var loaderComp))
            {
                if (_cartridgeLoader.TryGetProgram<BountyContractsCartridgeComponent>(loaderUid, out _, out var cartComp, true, loaderComp)
                    && cartComp.NotificationsEnabled)
                {
                    _cartridgeLoader.SendNotification(loaderUid, sender, msg, loaderComp);
                }
            }
        }
        else if (notificationType == BountyContractNotificationType.Radio)
        {
            var sender = Loc.GetString("bounty-contracts-announcement-radio-name");
            var target = !string.IsNullOrEmpty(contract.Vessel) && contract.Vessel != Loc.GetString("bounty-contracts-ui-create-vessel-unknown")
                ? $"{contract.Name} ({contract.Vessel})"
                : contract.Name;
            var msg = Loc.GetString(announcement,
                ("target", target), ("reward", BankSystemExtensions.ToSpesoString(contract.Reward)));
            var color = Color.FromHex("#D7D7BE");
            _chat.DispatchGlobalAnnouncement(msg, sender, false, colorOverride: color);
        }

        _adminLog.Add(LogType.BountyContractCreated, $"{ToPrettyString(actor):actor} posted a {category} bounty with ID {contractId} in the {collection} collection for ${reward}: {description ?? ""}");

        return contract;
    }

    /// <summary>
    ///     Try to get a bounty contract by its id.
    /// </summary>
    public bool TryGetContract(uint contractId, [NotNullWhen(true)] out BountyContract? contract)
    {
        contract = null;
        var data = GetContracts();
        if (data == null || data.Contracts == null)
            return false;

        // Linear over # collections, should be a small set
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
    public IEnumerable<BountyContract> GetPermittedContracts(Entity<BountyContractsCartridgeComponent> cartridge, EntityUid loader, out ProtoId<BountyContractCollectionPrototype>? newCollection)
    {
        newCollection = null;
        var data = GetContracts();

        if (data == null || data.Contracts == null)
            return Enumerable.Empty<BountyContract>();

        if (cartridge.Comp.Collection != null)
        {
            if (data.Contracts.TryGetValue(cartridge.Comp.Collection.Value, out var contracts)
                && HasReadAccess(loader, cartridge.Comp.Collection.Value, data))
            {
                newCollection = cartridge.Comp.Collection.Value;
                return contracts.Values;
            }
        }

        foreach (var collection in data.Contracts.Keys)
        {
            if (HasReadAccess(loader, collection, data))
            {
                newCollection = collection;
                return data.Contracts[collection].Values;
            }
        }

        // No valid permitted contracts to get
        return Enumerable.Empty<BountyContract>();
    }

    /// <summary>
    ///     Try to remove bounty contract by its id.
    /// </summary>
    /// <returns>True if contract was found and removed.</returns>
    public bool TryRemoveBountyContract(EntityUid authorUid, EntityUid actor, uint contractId)
    {
        var data = GetContracts();
        if (data == null || data.Contracts == null)
            return false;

        foreach (var (collectionId, collection) in data.Contracts)
        {
            if (!collection.TryGetValue(contractId, out var contract))
                continue;

            if (!HasDeleteAccess(authorUid, collectionId, data) && authorUid != GetEntity(contract.AuthorUid))
                return false;

            collection.Remove(contractId);
            _adminLog.Add(LogType.BountyContractRemoved, $"{ToPrettyString(actor):actor} deleted bounty with ID {contractId}");
            return true;
        }

        _sawmill.Warning($"Failed to remove bounty contract with {contractId}!");
        return false;
    }

    public override void Update(float frameTime)
    {
        var cartList = EntityQueryEnumerator<BountyContractsCartridgeComponent>();
        while (cartList.MoveNext(out var loaderUid, out var cartComponent))
        {
            if (cartComponent.CreateEnabled)
                continue;

            if (_timing.CurTime >= cartComponent.NextCreate)
            {
                cartComponent.CreateEnabled = true;
                // TODO: update UI if on the create menu
            }
        }
    }
}
