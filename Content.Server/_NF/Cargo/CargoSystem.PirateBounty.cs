using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server._NF.Contraband.Components;
using Content.Server._NF.Pirate.Components;
using Content.Server.Labels;
using Content.Shared._NF.Bank;
using Content.Shared._NF.Pirate;
using Content.Shared._NF.Pirate.Components;
using Content.Shared._NF.Pirate.Prototypes;
using Content.Shared._NF.Pirate.Events;
using Content.Shared.Access.Components;
using Content.Shared.Database;
using Content.Shared.NameIdentifier;
using Content.Shared.Paper;
using Content.Shared.Whitelist;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Cargo.Systems; // Needs to collide with base namespace

public sealed partial class CargoSystem
{
    [ValidatePrototypeId<NameIdentifierGroupPrototype>]
    private const string PirateBountyNameIdentifierGroup = "Bounty"; // Use the bounty name ID group (0-999) for now.

    private EntityQuery<PirateBountyLabelComponent> _pirateBountyLabelQuery;
    [Dependency] private EntProtoIdWhitelistSystem _entProtoIdWhitelist = default!;

    private readonly TimeSpan _redemptionDelay = TimeSpan.FromSeconds(2);

    private void InitializePirateBounty()
    {
        SubscribeLocalEvent<PirateBountyConsoleComponent, BoundUIOpenedEvent>(OnPirateBountyConsoleOpened);
        SubscribeLocalEvent<PirateBountyConsoleComponent, PirateBountyAcceptMessage>(OnPirateBountyAccept);
        SubscribeLocalEvent<PirateBountyConsoleComponent, PirateBountySkipMessage>(OnSkipPirateBountyMessage);

        SubscribeLocalEvent<PirateBountyRedemptionConsoleComponent, PirateBountyRedemptionMessage>(OnRedeemBounty);

        SubscribeLocalEvent<SectorPirateBountyDatabaseComponent, MapInitEvent>(OnPirateMapInit);

        _pirateBountyLabelQuery = GetEntityQuery<PirateBountyLabelComponent>();
    }

    private void OnPirateBountyConsoleOpened(EntityUid uid, PirateBountyConsoleComponent component, BoundUIOpenedEvent args)
    {
        var service = _sectorService.GetServiceEntity();
        if (!TryComp<SectorPirateBountyDatabaseComponent>(service, out var bountyDb))
        {
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
            _audio.PlayPvs(component.SpawnChestSound, uid);
        }
        else
        {
            var label = Spawn(component.BountyLabelId, Transform(uid).Coordinates);
            SetupPirateBountyManifest(label, bountyData, bountyPrototype);
            _audio.PlayPvs(component.PrintSound, uid);
        }

        component.NextPrintTime = _timing.CurTime + component.PrintDelay;
        UpdatePirateBountyConsoles();
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
        message.TryAddMarkup(Loc.GetString("pirate-bounty-chest-description-start"), out var _);
        foreach (var entry in prototype.Entries)
        {
            message.PushNewline();
            message.TryAddMarkup($"- {Loc.GetString("pirate-bounty-console-manifest-entry",
                ("amount", entry.Amount),
                ("item", Loc.GetString(entry.Name)))}", out var _);
        }
        message.PushNewline();
        message.TryAddMarkup(Loc.GetString("pirate-bounty-console-manifest-reward", ("reward", BankSystemExtensions.ToDoubloonString(prototype.Reward))), out var _);

        _metaSystem.SetEntityDescription(uid, message.ToMarkup());

        if (TryComp<PirateBountyLabelComponent>(uid, out var label))
            label.Id = bounty.Id;
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
            msg.TryAddMarkup($"- {Loc.GetString("pirate-bounty-console-manifest-entry",
                ("amount", entry.Amount),
                ("item", Loc.GetString(entry.Name)))}", out var _);
            msg.PushNewline();
        }
        msg.TryAddMarkup(Loc.GetString("pirate-bounty-console-manifest-reward", ("reward", BankSystemExtensions.ToDoubloonString(prototype.Reward))), out var _);
        _paperSystem.SetContent((uid, paper), msg.ToMarkup());
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

        UpdatePirateBountyConsoles();
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

    private void OnRedeemBounty(EntityUid uid, PirateBountyRedemptionConsoleComponent component, PirateBountyRedemptionMessage args)
    {
        var amount = 0;

        // Component still cooling down.
        if (component.LastRedeemAttempt + _redemptionDelay > _timing.CurTime)
            return;

        EntityUid gridUid = Transform(uid).GridUid ?? EntityUid.Invalid;
        if (gridUid == EntityUid.Invalid)
            return;

        // 1. Separate out accepted crate and non-crate bounties.  Create a tracker for non-crate bounties.
        if (!TryComp<SectorPirateBountyDatabaseComponent>(_sectorService.GetServiceEntity(), out var bountyDb))
            return;

        PirateBountyEntitySearchState bountySearchState = new PirateBountyEntitySearchState();

        foreach (var bounty in bountyDb.Bounties)
        {
            if (bounty.Accepted)
            {
                if (!_protoMan.TryIndex(bounty.Bounty, out var bountyPrototype))
                    continue;
                if (bountyPrototype.SpawnChest)
                {
                    var newState = new PirateBountyState(bounty, bountyPrototype);
                    foreach (var entry in bountyPrototype.Entries)
                    {
                        newState.Entries[entry.Name] = 0;
                    }
                    bountySearchState.CrateBounties[bounty.Id] = newState;
                }
                else
                {
                    var newState = new PirateBountyState(bounty, bountyPrototype);
                    foreach (var entry in bountyPrototype.Entries)
                    {
                        newState.Entries[entry.Name] = 0;
                    }
                    bountySearchState.LooseObjectBounties[bounty.Id] = newState;
                }
            }
        }

        // 2. Iterate over bounty pads, find all tagged, non-tagged items.
        foreach (var (palletUid, _) in GetContrabandPallets(gridUid))
        {
            foreach (var ent in _lookup.GetEntitiesIntersecting(palletUid,
                         LookupFlags.Dynamic | LookupFlags.Sundries | LookupFlags.Approximate))
            {
                // Dont match:
                // - anything anchored (e.g. light fixtures)
                // Checks against already handled set done by CheckEntityForPirateBounties
                if (_xformQuery.TryGetComponent(ent, out var xform) &&
                    xform.Anchored)
                {
                    continue;
                }

                CheckEntityForPirateBounties(ent, ref bountySearchState);
            }
        }

        // 4. When done, note all completed bounties.  Remove them from the list of accepted bounties, and spawn the rewards.
        bool bountiesRemoved = false;
        string redeemedBounties = string.Empty;
        foreach (var (id, bounty) in bountySearchState.CrateBounties)
        {
            bool bountyMet = true;
            var prototype = bounty.Prototype;
            foreach (var entry in prototype.Entries)
            {
                if (!bounty.Entries.ContainsKey(entry.Name) ||
                    entry.Amount > bounty.Entries[entry.Name])
                {
                    bountyMet = false;
                    break;
                }
            }

            if (bountyMet)
            {
                bountiesRemoved = true;
                redeemedBounties = Loc.GetString("pirate-bounty-redemption-append", ("bounty", id), ("empty", string.IsNullOrEmpty(redeemedBounties) ? 0 : 1), ("prev", redeemedBounties));

                TryRemovePirateBounty(_sectorService.GetServiceEntity(), id);
                amount += prototype.Reward;
                foreach (var entity in bounty.Entities)
                {
                    Del(entity);
                }
            }
        }

        foreach (var (id, bounty) in bountySearchState.LooseObjectBounties)
        {
            bool bountyMet = true;
            var prototype = bounty.Prototype;
            foreach (var entry in prototype.Entries)
            {
                if (!bounty.Entries.ContainsKey(entry.Name) ||
                    entry.Amount > bounty.Entries[entry.Name])
                {
                    bountyMet = false;
                    break;
                }
            }

            if (bountyMet)
            {
                bountiesRemoved = true;
                redeemedBounties = Loc.GetString("pirate-bounty-redemption-append", ("bounty", id), ("empty", string.IsNullOrEmpty(redeemedBounties) ? 0 : 1), ("prev", redeemedBounties));

                TryRemovePirateBounty(_sectorService.GetServiceEntity(), id);
                amount += prototype.Reward;
                foreach (var entity in bounty.Entities)
                {
                    Del(entity);
                }
            }
        }

        if (amount > 0)
        {
            // TODO: play a sound here, ideally the "deposit money" chime used on ATMs
            _stack.SpawnMultiple("Doubloon", amount, Transform(uid).Coordinates);
            _audio.PlayPvs(component.AcceptSound, uid);
            _popup.PopupEntity(Loc.GetString("pirate-bounty-redemption-success", ("bounties", redeemedBounties), ("amount", amount)), args.Actor);
        }
        else
        {
            _audio.PlayPvs(component.DenySound, uid);
            _popup.PopupEntity(Loc.GetString("pirate-bounty-redemption-deny"), args.Actor);
        }

        // Bounties removed, restore database list
        if (bountiesRemoved)
        {
            FillPirateBountyDatabase(_sectorService.GetServiceEntity());
        }
        component.LastRedeemAttempt = _timing.CurTime;
    }

    class PirateBountyState
    {
        public readonly PirateBountyData Data;
        public PirateBountyPrototype Prototype;
        public HashSet<EntityUid> Entities = new();
        public Dictionary<string, int> Entries = new();
        public bool Calculating = false; // Relevant only for crate bounties (due to tree traversal)

        public PirateBountyState(PirateBountyData data, PirateBountyPrototype prototype)
        {
            Data = data;
            Prototype = prototype;
        }
    }

    class PirateBountyEntitySearchState
    {
        public HashSet<EntityUid> HandledEntities = new();
        public Dictionary<string, PirateBountyState> LooseObjectBounties = new();
        public Dictionary<string, PirateBountyState> CrateBounties = new();
    }

    private void CheckEntityForPirateCrateBounty(EntityUid uid, ref PirateBountyEntitySearchState state, string id)
    {
        // Sanity check: entity previously handled, this subtree is done.
        if (state.HandledEntities.Contains(uid))
            return;

        // Add this container to the list of entities to remove.
        var bounty = state.CrateBounties[id]; // store the particular bounty we're looking up.
        if (bounty.Calculating) // Bounty check is already happening in a parent, return.
        {
            state.HandledEntities.Add(uid);
            return;
        }

        if (TryComp<ContainerManagerComponent>(uid, out var containers))
        {
            bounty.Entities.Add(uid);
            bounty.Calculating = true;

            foreach (var container in containers.Containers.Values)
            {
                foreach (var ent in container.ContainedEntities)
                {
                    // Subtree has a separate label, run check on that label
                    if (TryComp<PirateBountyLabelComponent>(ent, out var label))
                    {
                        CheckEntityForPirateCrateBounty(ent, ref state, label.Id);
                    }
                    else
                    {
                        // Check entry against bounties
                        foreach (var entry in bounty.Prototype.Entries)
                        {
                            // Should add an assertion here, entry.Name should exist.
                            // Entry already fulfilled, skip this entity.
                            if (bounty.Entries[entry.Name] >= entry.Amount)
                            {
                                continue;
                            }

                            // Check whitelists for the pirate bounty.
                            if ((_whitelistSys.IsWhitelistPass(entry.Whitelist, ent) ||
                                _entProtoIdWhitelist.IsWhitelistPass(entry.IdWhitelist, ent)) &&
                                _whitelistSys.IsBlacklistFailOrNull(entry.Blacklist, ent))
                            {
                                bounty.Entries[entry.Name]++;
                                bounty.Entities.Add(ent);
                                break;
                            }
                        }
                        state.HandledEntities.Add(ent);
                    }
                }
            }
        }
        state.HandledEntities.Add(uid);
    }

    // Return two lists: a list of non-labelled entities (nodes), and a list of labelled entities (subtrees)
    private void CheckEntityForPirateBounties(EntityUid uid, ref PirateBountyEntitySearchState state)
    {
        // Entity previously handled, this subtree is done.
        if (state.HandledEntities.Contains(uid))
            return;

        // 3a. If tagged as labelled, check contents against crate bounties.  If it satisfies any of them, note it as solved.
        if (TryComp<PirateBountyLabelComponent>(uid, out var label))
            CheckEntityForPirateCrateBounty(uid, ref state, label.Id);
        else
        {
            // 3b. If not tagged as labelled, check contents against non-create bounties.  If it satisfies any of them, increase the quantity.
            foreach (var (id, bounty) in state.LooseObjectBounties)
            {
                foreach (var entry in bounty.Prototype.Entries)
                {
                    // Should add an assertion here, entry.Name should exist.
                    // Entry already fulfilled, skip this entity.
                    if (bounty.Entries[entry.Name] >= entry.Amount)
                    {
                        continue;
                    }

                    // Check whitelists for the pirate bounty.
                    if ((_whitelistSys.IsWhitelistPass(entry.Whitelist, uid) ||
                        _entProtoIdWhitelist.IsWhitelistPass(entry.IdWhitelist, uid)) &&
                        _whitelistSys.IsBlacklistFailOrNull(entry.Blacklist, uid))
                    {
                        bounty.Entries[entry.Name]++;
                        bounty.Entities.Add(uid);
                        state.HandledEntities.Add(uid);
                        return;
                    }
                }
            }
        }
        state.HandledEntities.Add(uid);
    }
}
