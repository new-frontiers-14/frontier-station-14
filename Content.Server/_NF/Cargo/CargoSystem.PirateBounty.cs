using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Cargo.Components;
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
using JetBrains.Annotations;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Cargo.Systems; // Needs to collide with base namespace

public sealed partial class CargoSystem
{
    [ValidatePrototypeId<NameIdentifierGroupPrototype>]
    private const string PirateBountyNameIdentifierGroup = "PirateBounty";

    private EntityQuery<PirateBountyLabelComponent> _pirateBountyLabelQuery;

    // GROSS.
    private void InitializePirateBounty()
    {
        SubscribeLocalEvent<PirateBountyConsoleComponent, BoundUIOpenedEvent>(OnPirateBountyConsoleOpened);
        SubscribeLocalEvent<PirateBountyConsoleComponent, BountyPrintLabelMessage>(OnPiratePrintLabelMessage);
        SubscribeLocalEvent<PirateBountyConsoleComponent, BountySkipMessage>(OnSkipPirateBountyMessage);
        SubscribeLocalEvent<PirateBountyLabelComponent, PriceCalculationEvent>(OnGetPirateBountyPrice); // TODO: figure out how these labels interact with the chest (does the chest RECEIVE the label component?)
        //SubscribeLocalEvent<EntitySoldEvent>(OnPirateSold); // FIXME: figure this out
        SubscribeLocalEvent<StationCargoBountyDatabaseComponent, MapInitEvent>(OnPirateMapInit);

        _pirateBountyLabelQuery = GetEntityQuery<PirateBountyLabelComponent>();
    }

    private void OnPirateBountyConsoleOpened(EntityUid uid, PirateBountyConsoleComponent component, BoundUIOpenedEvent args)
    {
        if (_station.GetOwningStation(uid) is not { } station ||
            !TryComp<StationCargoBountyDatabaseComponent>(station, out var bountyDb))
            return;

        var untilNextSkip = bountyDb.NextSkipTime - _timing.CurTime;
        _uiSystem.SetUiState(uid, CargoConsoleUiKey.Bounty, new CargoBountyConsoleState(bountyDb.Bounties, untilNextSkip));
    }

    private void OnPiratePrintLabelMessage(EntityUid uid, PirateBountyConsoleComponent component, BountyPrintLabelMessage args)
    {
        if (_timing.CurTime < component.NextPrintTime)
            return;

        if (_station.GetOwningStation(uid) is not { } station)
            return;

        if (!TryGetBountyFromId(station, args.BountyId, out var bounty))
            return;

        var label = Spawn(component.BountyLabelId, Transform(uid).Coordinates);
        component.NextPrintTime = _timing.CurTime + component.PrintDelay;
        SetupBountyLabel(label, station, bounty.Value);
        _audio.PlayPvs(component.PrintSound, uid);
    }

    private void OnSkipPirateBountyMessage(EntityUid uid, PirateBountyConsoleComponent component, BountySkipMessage args)
    {
        if (_station.GetOwningStation(uid) is not { } station || !TryComp<StationCargoBountyDatabaseComponent>(station, out var db))
            return;

        if (_timing.CurTime < db.NextSkipTime)
            return;

        if (!TryGetBountyFromId(station, args.BountyId, out var bounty))
            return;

        if (args.Actor is not { Valid: true } mob)
            return;

        if (TryComp<AccessReaderComponent>(uid, out var accessReaderComponent) &&
            !_accessReaderSystem.IsAllowed(mob, uid, accessReaderComponent))
        {
            _audio.PlayPvs(component.DenySound, uid);
            return;
        }

        if (!TryRemoveBounty(station, bounty.Value))
            return;

        FillBountyDatabase(station);
        db.NextSkipTime = _timing.CurTime + db.SkipDelay;
        var untilNextSkip = db.NextSkipTime - _timing.CurTime;
        _uiSystem.SetUiState(uid, CargoConsoleUiKey.Bounty, new CargoBountyConsoleState(db.Bounties, untilNextSkip));
        _audio.PlayPvs(component.SkipSound, uid);
    }

    public void SetupPirateBountyLabel(EntityUid uid, EntityUid stationId, CargoBountyData bounty, PaperComponent? paper = null, CargoBountyLabelComponent? label = null)
    {
        if (!Resolve(uid, ref paper, ref label) || !_protoMan.TryIndex<CargoBountyPrototype>(bounty.Bounty, out var prototype))
            return;

        label.Id = bounty.Id;
        label.AssociatedStationId = stationId;
        var msg = new FormattedMessage();
        msg.AddText(Loc.GetString("bounty-manifest-header", ("id", bounty.Id)));
        msg.PushNewline();
        msg.AddText(Loc.GetString("bounty-manifest-list-start"));
        msg.PushNewline();
        foreach (var entry in prototype.Entries)
        {
            msg.AddMarkup($"- {Loc.GetString("bounty-console-manifest-entry",
                ("amount", entry.Amount),
                ("item", Loc.GetString(entry.Name)))}");
            msg.PushNewline();
        }
        _paperSystem.SetContent(uid, msg.ToMarkup(), paper);
    }

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

        if (component.AssociatedStationId is not { } station || !TryComp<StationPirateBountyDatabaseComponent>(station, out var database))
            return;

        if (database.CheckedBounties.Contains(component.Id))
            return;

        if (!TryGetPirateBountyFromId(station, component.Id, out var bounty, database))
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
        if (!TryGetBountyLabel(uid, out _, out var component))
            return false;

        if (component.AssociatedStationId is not { } station || !TryGetPirateBountyFromId(station, component.Id, out var bounty))
        {
            return false;
        }

        if (!IsPirateBountyComplete(uid, bounty.Value))
        {
            return false;
        }

        TryRemovePirateBounty(station, bounty.Value);
        FillPirateBountyDatabase(station);
        _adminLogger.Add(LogType.Action, LogImpact.Low, $"Bounty \"{bounty.Value.Bounty}\" (id:{bounty.Value.Id}) was fulfilled");
        return false;
    }

    private bool TryGetPirateBountyLabel(EntityUid uid,
        [NotNullWhen(true)] out EntityUid? labelEnt,
        [NotNullWhen(true)] out CargoBountyLabelComponent? labelComp)
    {
        labelEnt = null;
        labelComp = null;
        if (!_containerQuery.TryGetComponent(uid, out var containerMan))
            return false;

        // make sure this label was actually applied to a crate.
        if (!_container.TryGetContainer(uid, LabelSystem.ContainerName, out var container, containerMan))
            return false;

        if (container.ContainedEntities.FirstOrNull() is not { } label ||
            !_bountyLabelQuery.TryGetComponent(label, out var component))
            return false;

        labelEnt = label;
        labelComp = component;
        return true;
    }

    // TODO: Fix this stupid name
    private void OnPirateMapInit(EntityUid uid, StationCargoBountyDatabaseComponent component, MapInitEvent args)
    {
        FillPirateBountyDatabase(uid, component);
    }

    /// <summary>
    /// Fills up the bounty database with random bounties.
    /// </summary>
    public void FillPirateBountyDatabase(EntityUid uid, StationCargoBountyDatabaseComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        while (component.Bounties.Count < component.MaxBounties)
        {
            if (!TryAddBounty(uid, component))
                break;
        }

        UpdateBountyConsoles();
    }

    public void RerollPirateBountyDatabase(Entity<StationCargoBountyDatabaseComponent?> entity)
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

        var station = component.AssociatedStationId;
        if (station == null)
        {
            bountyEntities = new();
            return false;
        }

        if (!TryGetPirateBountyFromId(station.Value, component.Id, out var bounty))
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
                if (!entry.Whitelist.IsValid(entity, EntityManager))
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
    public bool TryAddPirateBounty(EntityUid uid, StationCargoBountyDatabaseComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        // todo: consider making the cargo bounties weighted.
        var allBounties = _protoMan.EnumeratePrototypes<CargoBountyPrototype>().ToList();
        var filteredBounties = new List<CargoBountyPrototype>();
        foreach (var proto in allBounties)
        {
            if (component.Bounties.Any(b => b.Bounty == proto.ID))
                continue;
            filteredBounties.Add(proto);
        }

        var pool = filteredBounties.Count == 0 ? allBounties : filteredBounties;
        var bounty = _random.Pick(pool);
        return TryAddBounty(uid, bounty, component);
    }

    [PublicAPI]
    public bool TryAddPirateBounty(EntityUid uid, string bountyId, StationPirateBountyDatabaseComponent? component = null)
    {
        if (!_protoMan.TryIndex<PirateBountyPrototype>(bountyId, out var bounty))
        {
            return false;
        }

        return TryAddPirateBounty(uid, bounty, component);
    }

    public bool TryAddPirateBounty(EntityUid uid, PirateBountyPrototype bounty, StationPirateBountyDatabaseComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (component.Bounties.Count >= component.MaxBounties)
            return false;

        _nameIdentifier.GenerateUniqueName(uid, BountyNameIdentifierGroup, out var randomVal); // Need a string ID for internal name, probably doesn't need to be outward facing.
        component.Bounties.Add(new PirateBountyData(bounty, randomVal));
        _adminLogger.Add(LogType.Action, LogImpact.Low, $"Added bounty \"{bounty.ID}\" (id:{component.TotalBounties}) to station {ToPrettyString(uid)}");
        component.TotalBounties++;
        return true;
    }

    [PublicAPI]
    public bool TryRemovePirateBounty(EntityUid uid, string dataId, StationPirateBountyDatabaseComponent? component = null)
    {
        if (!TryGetPirateBountyFromId(uid, dataId, out var data, component))
            return false;

        return TryRemovePirateBounty(uid, data.Value, component);
    }

    public bool TryRemovePirateBounty(EntityUid uid, PirateBountyData data, StationPirateBountyDatabaseComponent? component = null)
    {
        if (!Resolve(uid, ref component))
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
        StationPirateBountyDatabaseComponent? component = null)
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

    public void UpdatePirateBountyConsoles()
    {
        var query = EntityQueryEnumerator<CargoBountyConsoleComponent, UserInterfaceComponent>();
        while (query.MoveNext(out var uid, out _, out var ui))
        {
            if (_station.GetOwningStation(uid) is not { } station ||
                !TryComp<StationPirateBountyDatabaseComponent>(station, out var db))
            {
                continue;
            }

            var untilNextSkip = db.NextSkipTime - _timing.CurTime;
            _uiSystem.SetUiState((uid, ui), CargoConsoleUiKey.Bounty, new PirateBountyConsoleState(db.Bounties, untilNextSkip));
        }
    }

    private void UpdatePirateBounty()
    {
        var query = EntityQueryEnumerator<StationPirateBountyDatabaseComponent>();
        while (query.MoveNext(out var bountyDatabase))
        {
            bountyDatabase.CheckedBounties.Clear();
        }
    }
}
