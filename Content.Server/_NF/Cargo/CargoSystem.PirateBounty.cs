using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server._NF.Pirate.Components;
using Content.Server.Botany.Components;
using Content.Server.Chat.V2;
using Content.Server.Labels;
using Content.Server.NameIdentifier;
using Content.Server.Paper;
using Content.Shared._NF.Pirate;
using Content.Shared._NF.Pirate.Components;
using Content.Shared._NF.Pirate.Prototypes;
using Content.Shared.Access.Components;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Database;
using Content.Shared.NameIdentifier;
using Content.Shared.Stacks;
using FastAccessors;
using JetBrains.Annotations;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Cargo.Systems; // Needs to collide with base namespace

public sealed partial class CargoSystem
{
    [ValidatePrototypeId<NameIdentifierGroupPrototype>]
    private const string PirateBountyNameIdentifierGroup = "Bounty"; // Use the bounty name ID group (0-999) for now.

    private EntityQuery<PirateBountyLabelComponent> _pirateBountyLabelQuery;

    // GROSS.
    private void InitializePirateBounty()
    {
        SubscribeLocalEvent<PirateBountyConsoleComponent, BoundUIOpenedEvent>(OnPirateBountyConsoleOpened);
        SubscribeLocalEvent<PirateBountyConsoleComponent, PirateBountyAcceptMessage>(OnPirateBountyAccept);
        SubscribeLocalEvent<PirateBountyConsoleComponent, PirateBountySkipMessage>(OnSkipPirateBountyMessage);
        SubscribeLocalEvent<PirateBountyLabelComponent, PriceCalculationEvent>(OnGetPirateBountyPrice); // TODO: figure out how these labels interact with the chest (does the chest RECEIVE the label component?)
        //SubscribeLocalEvent<EntitySoldEvent>(OnPirateSold); // FIXME: figure this out
        SubscribeLocalEvent<SectorPirateBountyDatabaseComponent, MapInitEvent>(OnPirateMapInit);

        _pirateBountyLabelQuery = GetEntityQuery<PirateBountyLabelComponent>();
    }

    private void OnPirateBountyConsoleOpened(EntityUid uid, PirateBountyConsoleComponent component, BoundUIOpenedEvent args)
    {
        Log.Error("OnPirateBountyConsoleOpened!");
        var service = _sectorService.GetServiceEntity();
        if (!TryComp<SectorPirateBountyDatabaseComponent>(service, out var bountyDb))
        {
            Log.Error("ConsoleOpened: no DB!");
            return;
        }

        var untilNextSkip = bountyDb.NextSkipTime - _timing.CurTime;
        _uiSystem.SetUiState(uid, PirateConsoleUiKey.Bounty, new PirateBountyConsoleState(bountyDb.Bounties, untilNextSkip));
    }

    private void OnPirateBountyAccept(EntityUid uid, PirateBountyConsoleComponent component, PirateBountyAcceptMessage args)
    {
        if (_timing.CurTime < component.NextPrintTime)
            return;

        var service = _sectorService.GetServiceEntity();
        if (!TryGetPirateBountyFromId(service, args.BountyId, out var bounty))
            return;

        var bountyObj = bounty.Value;

        // Check if the crate for this bounty has already been summoned.  If not, create a new one.
        if (bountyObj.Accepted || !_protoMan.TryIndex(bountyObj.Bounty, out var bountyPrototype))
            return;

        PirateBountyData bountyData = new PirateBountyData(bountyPrototype!, bountyObj.Id, true);

        TryOverwritePirateBountyFromId(service, bountyData);

        if (bountyPrototype.SpawnChest)
        {
            var chest = Spawn(component.BountyCrateId, Transform(uid).Coordinates);
            SetupPirateBountyChest(chest, bountyData, bountyPrototype);
        }
        else
        {
            var label = Spawn(component.BountyLabelId, Transform(uid).Coordinates);
            SetupPirateBountyManifest(label, bountyData, bountyPrototype);
        }

        component.NextPrintTime = _timing.CurTime + component.PrintDelay;
        _audio.PlayPvs(component.PrintSound, uid);
        UpdateBountyConsoles();
    }

    private void OnSkipPirateBountyMessage(EntityUid uid, PirateBountyConsoleComponent component, PirateBountySkipMessage args)
    {
        var service = _sectorService.GetServiceEntity();
        if (!TryComp<SectorPirateBountyDatabaseComponent>(service, out var db))
            return;

        if (_timing.CurTime < db.NextSkipTime)
            return;

        if (!TryGetPirateBountyFromId(service, args.BountyId, out var bounty))
            return;

        if (args.Actor is not { Valid: true } mob)
            return;

        if (TryComp<AccessReaderComponent>(uid, out var accessReaderComponent) &&
            !_accessReaderSystem.IsAllowed(mob, uid, accessReaderComponent))
        {
            _audio.PlayPvs(component.DenySound, uid);
            return;
        }

        if (!TryRemovePirateBounty(service, bounty.Value.Id))
            return;

        FillPirateBountyDatabase(service);
        if (bounty.Value.Accepted)
            db.NextSkipTime = _timing.CurTime + db.SkipDelay;
        else
            db.NextSkipTime = _timing.CurTime + db.CancelDelay;

        var untilNextSkip = db.NextSkipTime - _timing.CurTime;
        _uiSystem.SetUiState(uid, PirateConsoleUiKey.Bounty, new PirateBountyConsoleState(db.Bounties, untilNextSkip));
        _audio.PlayPvs(component.SkipSound, uid);
    }

    private void SetupPirateBountyChest(EntityUid uid, PirateBountyData bounty, PirateBountyPrototype prototype)
    {
        _metaSystem.SetEntityName(uid, Loc.GetString("pirate-bounty-chest-name", ("id", bounty.Id)));

        FormattedMessage message = new FormattedMessage();
        message.AddMarkup(Loc.GetString("pirate-bounty-chest-description-start"));
        foreach (var entry in prototype.Entries)
        {
            message.PushNewline();
            message.AddMarkup($"- {Loc.GetString("pirate-bounty-console-manifest-entry",
                ("amount", entry.Amount),
                ("item", Loc.GetString(entry.Name)))}");
        }
        message.PushNewline();
        message.AddMarkup(Loc.GetString("pirate-bounty-console-manifest-reward", ("reward", prototype.Reward)));

        _metaSystem.SetEntityDescription(uid, message.ToMarkup());
    }

    private void SetupPirateBountyManifest(EntityUid uid, PirateBountyData bounty, PirateBountyPrototype prototype, PaperComponent? paper = null)
    {
        _metaSystem.SetEntityName(uid, Loc.GetString("pirate-bounty-manifest-name", ("id", bounty.Id)));

        if (!Resolve(uid, ref paper))
            return;

        var msg = new FormattedMessage();
        msg.AddText(Loc.GetString("pirate-bounty-manifest-header", ("id", bounty.Id)));
        msg.PushNewline();
        msg.AddText(Loc.GetString("pirate-bounty-manifest-list-start"));
        msg.PushNewline();
        foreach (var entry in prototype.Entries)
        {
            msg.AddMarkup($"- {Loc.GetString("pirate-bounty-console-manifest-entry",
                ("amount", entry.Amount),
                ("item", Loc.GetString(entry.Name)))}");
            msg.PushNewline();
        }
        msg.AddMarkup(Loc.GetString("pirate-bounty-console-manifest-reward", ("reward", prototype.Reward)));
        _paperSystem.SetContent(uid, msg.ToMarkup(), paper);
    }

    // TODO: rework this to include loose items off of a pallet
    /// <summary>
    /// Bounties do not sell for any currency. The reward for a bounty is
    /// calculated after it is sold separately from the selling system.
    /// </summary>
    private void OnGetPirateBountyPrice(EntityUid uid, PirateBountyLabelComponent component, ref PriceCalculationEvent args)
    {
        if (args.Handled || component.Calculating)
            return;

        // make sure this label was actually applied to a crate.
        if (!_container.TryGetContainingContainer(uid, out var container) || container.ID != LabelSystem.ContainerName)
            return;

        var serviceId = _sectorService.GetServiceEntity();
        if (!TryComp<SectorPirateBountyDatabaseComponent>(serviceId, out var database))
            return;

        if (database.CheckedBounties.Contains(component.Id))
            return;

        if (!TryGetPirateBountyFromId(serviceId, component.Id, out var bounty, database))
            return;

        if (!_protoMan.TryIndex(bounty.Value.Bounty, out var bountyPrototype) ||
            !IsPirateBountyComplete(container.Owner, bountyPrototype))
            return;

        database.CheckedBounties.Add(component.Id);
        args.Handled = true;

        component.Calculating = true;
        args.Price = bountyPrototype.Reward - _pricing.GetPrice(container.Owner);
        component.Calculating = false;
    }

    // Receives a bounty that's been sold.
    // Returns true if the bounty has been handled, false otherwise.
    private bool HandlePirateBounty(EntityUid uid)
    {
        if (!TryGetPirateBountyLabel(uid, out _, out var component))
            return false;

        var serviceId = _sectorService.GetServiceEntity();
        if (!TryGetPirateBountyFromId(serviceId, component.Id, out var bounty))
        {
            return false;
        }

        if (!IsPirateBountyComplete(uid, bounty.Value))
        {
            return false;
        }

        TryRemovePirateBounty(serviceId, bounty.Value.Id);
        FillPirateBountyDatabase(serviceId);
        _adminLogger.Add(LogType.Action, LogImpact.Low, $"Pirate bounty \"{bounty.Value.Bounty}\" (id:{bounty.Value.Id}) was fulfilled");
        return false;
    }

    private bool TryGetPirateBountyLabel(EntityUid uid,
        [NotNullWhen(true)] out EntityUid? labelEnt,
        [NotNullWhen(true)] out PirateBountyLabelComponent? labelComp)
    {
        labelEnt = null;
        labelComp = null;
        if (!_containerQuery.TryGetComponent(uid, out var containerMan))
            return false;

        // make sure this label was actually applied to a crate.
        if (!_container.TryGetContainer(uid, LabelSystem.ContainerName, out var container, containerMan))
            return false;

        if (container.ContainedEntities.FirstOrNull() is not { } label ||
            !_pirateBountyLabelQuery.TryGetComponent(label, out var component))
            return false;

        labelEnt = label;
        labelComp = component;
        return true;
    }

    private void OnPirateMapInit(EntityUid uid, SectorPirateBountyDatabaseComponent component, MapInitEvent args)
    {
        FillPirateBountyDatabase(uid, component);
    }

    /// <summary>
    /// Fills up the bounty database with random bounties.
    /// </summary>
    public void FillPirateBountyDatabase(EntityUid serviceId, SectorPirateBountyDatabaseComponent? component = null)
    {
        if (!Resolve(serviceId, ref component))
            return;

        while (component?.Bounties.Count < component?.MaxBounties)
        {
            if (!TryAddPirateBounty(serviceId, component))
                break;
        }

        UpdateBountyConsoles();
    }

    public void RerollPirateBountyDatabase(Entity<SectorPirateBountyDatabaseComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return;

        entity.Comp.Bounties.Clear();
        FillPirateBountyDatabase(entity);
    }

    public bool IsPirateBountyComplete(EntityUid container, out HashSet<EntityUid> bountyEntities)
    {
        if (!TryGetPirateBountyLabel(container, out _, out var component))
        {
            bountyEntities = new();
            return false;
        }

        var service = _sectorService.GetServiceEntity();
        if (!TryGetPirateBountyFromId(service, component.Id, out var bounty))
        {
            bountyEntities = new();
            return false;
        }

        return IsPirateBountyComplete(container, bounty.Value, out bountyEntities);
    }

    public bool IsPirateBountyComplete(EntityUid container, PirateBountyData data)
    {
        return IsPirateBountyComplete(container, data, out _);
    }

    public bool IsPirateBountyComplete(EntityUid container, PirateBountyData data, out HashSet<EntityUid> bountyEntities)
    {
        if (!_protoMan.TryIndex(data.Bounty, out var proto))
        {
            bountyEntities = new();
            return false;
        }

        return IsPirateBountyComplete(container, proto.Entries, out bountyEntities);
    }

    public bool IsPirateBountyComplete(EntityUid container, string id)
    {
        if (!_protoMan.TryIndex<PirateBountyPrototype>(id, out var proto))
            return false;

        return IsPirateBountyComplete(container, proto.Entries);
    }

    public bool IsPirateBountyComplete(EntityUid container, PirateBountyPrototype prototype)
    {
        return IsPirateBountyComplete(container, prototype.Entries);
    }

    public bool IsPirateBountyComplete(EntityUid container, IEnumerable<PirateBountyItemEntry> entries)
    {
        return IsPirateBountyComplete(container, entries, out _);
    }

    public bool IsPirateBountyComplete(EntityUid container, IEnumerable<PirateBountyItemEntry> entries, out HashSet<EntityUid> bountyEntities)
    {
        return IsPirateBountyComplete(GetBountyEntities(container), entries, out bountyEntities);
    }

    public bool IsPirateBountyComplete(HashSet<EntityUid> entities, IEnumerable<PirateBountyItemEntry> entries, out HashSet<EntityUid> bountyEntities)
    {
        bountyEntities = new();

        foreach (var entry in entries)
        {
            var count = 0;

            // store entities that already satisfied an
            // entry so we don't double-count them.
            var temp = new HashSet<EntityUid>();
            foreach (var entity in entities)
            {
                if (_whitelistSystem.IsWhitelistFailOrNull(entry.Whitelist, entity))
                    continue;

                count += _stackQuery.CompOrNull(entity)?.Count ?? 1;
                temp.Add(entity);

                if (count >= entry.Amount)
                    break;
            }

            if (count < entry.Amount)
                return false;

            foreach (var ent in temp)
            {
                entities.Remove(ent);
                bountyEntities.Add(ent);
            }
        }

        return true;
    }

    private HashSet<EntityUid> GetPirateBountyEntities(EntityUid uid)
    {
        var entities = new HashSet<EntityUid>
        {
            uid
        };
        if (!TryComp<ContainerManagerComponent>(uid, out var containers))
            return entities;

        foreach (var container in containers.Containers.Values)
        {
            foreach (var ent in container.ContainedEntities)
            {
                if (_bountyLabelQuery.HasComponent(ent))
                    continue;

                var children = GetPirateBountyEntities(ent);
                foreach (var child in children)
                {
                    entities.Add(child);
                }
            }
        }

        return entities;
    }

    [PublicAPI]
    public bool TryAddPirateBounty(EntityUid serviceId, SectorPirateBountyDatabaseComponent? component = null)
    {
        if (!Resolve(serviceId, ref component))
            return false;

        // todo: consider making the pirate bounties weighted.
        var allBounties = _protoMan.EnumeratePrototypes<PirateBountyPrototype>().ToList();
        var filteredBounties = new List<PirateBountyPrototype>();
        foreach (var proto in allBounties)
        {
            if (component.Bounties.Any(b => b.Bounty == proto.ID))
                continue;
            filteredBounties.Add(proto);
        }

        var pool = filteredBounties.Count == 0 ? allBounties : filteredBounties;
        var bounty = _random.Pick(pool);
        return TryAddPirateBounty(serviceId, bounty, component);
    }

    [PublicAPI]
    public bool TryAddPirateBounty(EntityUid serviceId, string bountyId, SectorPirateBountyDatabaseComponent? component = null)
    {
        if (!_protoMan.TryIndex<PirateBountyPrototype>(bountyId, out var bounty))
        {
            return false;
        }

        return TryAddPirateBounty(serviceId, bounty, component);
    }

    public bool TryAddPirateBounty(EntityUid serviceId, PirateBountyPrototype bounty, SectorPirateBountyDatabaseComponent? component = null)
    {
        if (!Resolve(serviceId, ref component))
            return false;

        if (component.Bounties.Count >= component.MaxBounties)
            return false;

        _nameIdentifier.GenerateUniqueName(serviceId, PirateBountyNameIdentifierGroup, out var randomVal); // Need a string ID for internal name, probably doesn't need to be outward facing.
        component.Bounties.Add(new PirateBountyData(bounty, randomVal, false));
        _adminLogger.Add(LogType.Action, LogImpact.Low, $"Added pirate bounty \"{bounty.ID}\" (id:{component.TotalBounties}) to service {ToPrettyString(serviceId)}");
        component.TotalBounties++;
        return true;
    }

    [PublicAPI]
    public bool TryRemovePirateBounty(EntityUid serviceId, string dataId, SectorPirateBountyDatabaseComponent? component = null)
    {
        if (!TryGetPirateBountyFromId(serviceId, dataId, out var data, component))
            return false;

        return TryRemovePirateBounty(serviceId, data.Value, component);
    }

    public bool TryRemovePirateBounty(EntityUid serviceId, PirateBountyData data, SectorPirateBountyDatabaseComponent? component = null)
    {
        if (!Resolve(serviceId, ref component))
            return false;

        for (var i = 0; i < component.Bounties.Count; i++)
        {
            if (component.Bounties[i].Id == data.Id)
            {
                component.Bounties.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    public bool TryGetPirateBountyFromId(
        EntityUid uid,
        string id,
        [NotNullWhen(true)] out PirateBountyData? bounty,
        SectorPirateBountyDatabaseComponent? component = null)
    {
        bounty = null;
        if (!Resolve(uid, ref component))
            return false;

        foreach (var bountyData in component.Bounties)
        {
            if (bountyData.Id != id)
                continue;
            bounty = bountyData;
            break;
        }

        return bounty != null;
    }

    private bool TryOverwritePirateBountyFromId(
        EntityUid uid,
        PirateBountyData bounty,
        SectorPirateBountyDatabaseComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        for (int i = 0; i < component.Bounties.Count; i++)
        {
            if (bounty.Id == component.Bounties[i].Id)
            {
                component.Bounties[i] = bounty;
                return true;
            }
        }
        return false;
    }

    public void UpdatePirateBountyConsoles()
    {
        var query = EntityQueryEnumerator<PirateBountyConsoleComponent, UserInterfaceComponent>();

        var serviceId = _sectorService.GetServiceEntity();
        if (!TryComp<SectorPirateBountyDatabaseComponent>(serviceId, out var db))
            return;

        while (query.MoveNext(out var uid, out _, out var ui))
        {
            var untilNextSkip = db.NextSkipTime - _timing.CurTime;
            _uiSystem.SetUiState((uid, ui), PirateConsoleUiKey.Bounty, new PirateBountyConsoleState(db.Bounties, untilNextSkip));
        }
    }

    private void UpdatePirateBounty()
    {
        var serviceId = _sectorService.GetServiceEntity();
        if (!TryComp<SectorPirateBountyDatabaseComponent>(serviceId, out var bountyDatabase))
            return;
        bountyDatabase.CheckedBounties.Clear();
    }

    private void OnPalletAppraise(EntityUid uid, ContrabandPalletConsoleComponent component, ContrabandPalletAppraiseMessage args)
    {
        if (args.Actor is null)
            return;

        UpdatePalletConsoleInterface(uid, component);
    }

    private List<(EntityUid Entity, ContrabandPalletComponent Component)> GetContrabandPallets(EntityUid gridUid)
    {
        var pads = new List<(EntityUid, ContrabandPalletComponent)>();
        var query = AllEntityQuery<ContrabandPalletComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var comp, out var compXform))
        {
            if (compXform.ParentUid != gridUid ||
                !compXform.Anchored)
            {
                continue;
            }

            pads.Add((uid, comp));
        }

        return pads;
    }

    // TODO: REMOVE ALL ITEMS FROM COMPLETED BOUNTIES
    private void SellPallets(EntityUid gridUid, ContrabandPalletConsoleComponent component, EntityUid? station, out int amount)
    {
        station ??= _station.GetOwningStation(gridUid);
        GetPalletGoods(gridUid, component, out var toSell, out amount);

        Log.Debug($"{component.Faction} sold {toSell.Count} contraband items for {amount}");

        if (station != null)
        {
            var ev = new EntitySoldEvent(toSell);
            RaiseLocalEvent(ref ev);
        }

        foreach (var ent in toSell)
        {
            Del(ent);
        }
    }

    private void GetPalletGoods(EntityUid gridUid, ContrabandPalletConsoleComponent console, out int amount)
    {
        amount = 0;

        // 1. Separate out accepted crate and non-crate bounties.  Create a tracker for non-crate bounties.
        if (!TryComp<SectorPirateBountyDatabaseComponent>(_sectorService.GetServiceEntity(), out var bountyDb))
            return;

        HashSet<PirateBountyPrototype> openCrateBounties = new HashSet<PirateBountyPrototype>();
        HashSet<PirateBountyPrototype> openLooseObjectBounties = new HashSet<PirateBountyPrototype>();

        foreach (var bounty in bountyDb.Bounties)
        {
            if (bounty.Accepted)
            {
                if (!_protoMan.TryIndex(bounty.Bounty, out var bountyPrototype))
                    continue;
                if (bountyPrototype.SpawnChest)
                    openCrateBounties.Add(bountyPrototype);
                else
                    openLooseObjectBounties.Add(bountyPrototype);
            }
        }

        // 2. Iterate over bounty pads, find all tagged, non-tagged items.
        var handledEntities = new HashSet<EntityUid>();
        var entitiesToRemove = new HashSet<EntityUid>();
        HashSet<PirateBountyPrototype> completedBounties = new HashSet<PirateBountyPrototype>();

        foreach (var (palletUid, _) in GetContrabandPallets(gridUid))
        {
            foreach (var ent in _lookup.GetEntitiesIntersecting(palletUid,
                         LookupFlags.Dynamic | LookupFlags.Sundries | LookupFlags.Approximate))
            {
                // Dont match:
                // - anything already being sold
                // - anything anchored (e.g. light fixtures)
                if (handledEntities.Contains(ent) ||
                    _xformQuery.TryGetComponent(ent, out var xform) &&
                    xform.Anchored)
                {
                    continue;
                }

                if (TryComp<PirateBountyLabelComponent>(ent, out var label))
                {
                    // 3a. If tagged as labelled, check contents against crate bounties.  If it satisfies any of them, note it as solved.
                    if (TryComp<ContainerManagerComponent>(ent, out var containers))
                    {
                        var entities = new HashSet<EntityUid>
                        {
                            ent
                        };

                        foreach (var container in containers.Containers.Values)
                        {
                            foreach (var item in container.ContainedEntities)
                            {
                                if (_bountyLabelQuery.HasComponent(item))
                                    continue;

                                var children = GetBountyEntities(item);
                                foreach (var child in children)
                                {
                                    entities.Add(child);
                                }
                            }
                        }

                    }
                    // if (!_containerQuery.TryGetComponent(uid, out var containerMan))
                    //     return false;

                    // // make sure this label was actually applied to a crate.
                    // if (!_container.TryGetContainer(uid, LabelSystem.ContainerName, out var container, containerMan))
                    //     return false;
                }
                else
                {
                    // 3b. If not tagged as labelled, check contents against non-create bounties.  If it satisfies any of them, increase the quantity.
                }
            }
        }

        // 4. When done, note all completed bounties.  Remove them from the list of accepted bounties, and spawn the rewards.






        // make sure this label was actually applied to a crate.
        // if (!_container.TryGetContainingContainer(uid, out var container) || container.ID != LabelSystem.ContainerName)
        //     return;

        if (component.AssociatedStationId is not { } station || !TryComp<StationCargoBountyDatabaseComponent>(station, out var database))
            return;

        if (database.CheckedBounties.Contains(component.Id))
            return;

        // if (!TryGetBountyFromId(station, component.Id, out var bounty, database))
        //     return;

        // if (!_protoMan.TryIndex(bounty.Value.Bounty, out var bountyPrototype) ||
        //     !IsBountyComplete(container.Owner, bountyPrototype))
        //     return;

        database.CheckedBounties.Add(component.Id);
        args.Handled = true;

        component.Calculating = true;
        args.Price = bountyPrototype.Reward - _pricing.GetPrice(container.Owner);
        component.Calculating = false;

        foreach (var (palletUid, _) in GetContrabandPallets(gridUid))
        {
            foreach (var ent in _lookup.GetEntitiesIntersecting(palletUid,
                         LookupFlags.Dynamic | LookupFlags.Sundries | LookupFlags.Approximate))
            {
                // Dont sell:
                // - anything already being sold
                // - anything anchored (e.g. light fixtures)
                // - anything blacklisted (e.g. players).
                if (toSell.Contains(ent) ||
                    _xformQuery.TryGetComponent(ent, out var xform) &&
                    (xform.Anchored || !CanSell(ent, xform)))
                {
                    continue;
                }

                if (_blacklistQuery.HasComponent(ent))
                    continue;

                if (TryComp<ContrabandComponent>(ent, out var comp) && comp.Currency == console.RewardType)
                {
                    if (comp.Value == 0)
                        continue;
                    toSell.Add(ent);
                    amount += comp.Value;
                }
            }
        }
    }

    private void OnPalletSale(EntityUid uid, ContrabandPalletConsoleComponent component, ContrabandPalletSellMessage args)
    {
        var player = args.Actor;

        if (player == null)
            return;

        if (Transform(uid).GridUid is not EntityUid gridUid)
        {
            _uiSystem.SetUiState(uid, ContrabandPalletConsoleUiKey.Contraband,
                new ContrabandPalletConsoleInterfaceState(0, 0, false));
            return;
        }

        SellPallets(gridUid, component, null, out var price);

        var stackPrototype = _protoMan.Index<StackPrototype>(component.RewardType);
        _stack.Spawn(price, stackPrototype, uid.ToCoordinates());
        UpdatePalletConsoleInterface(uid, component);
    }
    private void UpdatePalletConsoleInterface(EntityUid uid, ContrabandPalletConsoleComponent comp)
    {
        var bui = _uiSystem.HasUi(uid, ContrabandPalletConsoleUiKey.Contraband);
        if (Transform(uid).GridUid is not EntityUid gridUid)
        {
            _uiSystem.SetUiState(uid, ContrabandPalletConsoleUiKey.Contraband,
                new ContrabandPalletConsoleInterfaceState(0, 0, false));
            return;
        }

        GetPalletGoods(gridUid, comp, out var toSell, out var amount);

        _uiSystem.SetUiState(uid, ContrabandPalletConsoleUiKey.Contraband,
            new ContrabandPalletConsoleInterfaceState((int) amount, toSell.Count, true));
    }
}
