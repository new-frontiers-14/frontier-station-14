using Content.Server._NF.Contraband.Components;
using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Stack;
using Content.Server.Station.Systems;
using Content.Shared._NF.Contraband;
using Content.Shared._NF.Contraband.BUI;
using Content.Shared._NF.Contraband.Components;
using Content.Shared._NF.Contraband.Events;
using Content.Shared.Contraband;
using Content.Shared.Stacks;
using Robust.Server.GameObjects;
using Content.Shared.Coordinates;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.Contraband.Systems;

/// <summary>
/// Contraband system. Contraband Pallet UI Console is mostly a copy of the system in cargo. Checkraze Note: copy of my code from cargosystems.shuttles.cs
/// </summary>
public sealed partial class ContrabandTurnInSystem : SharedContrabandTurnInSystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly StackSystem _stack = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    private EntityQuery<MobStateComponent> _mobQuery;
    private EntityQuery<TransformComponent> _xformQuery;
    private EntityQuery<CargoSellBlacklistComponent> _blacklistQuery;

    public override void Initialize()
    {
        base.Initialize();

        _xformQuery = GetEntityQuery<TransformComponent>();
        _blacklistQuery = GetEntityQuery<CargoSellBlacklistComponent>();
        _mobQuery = GetEntityQuery<MobStateComponent>();

        SubscribeLocalEvent<ContrabandPalletConsoleComponent, ContrabandPalletSellMessage>(OnPalletSale);
        SubscribeLocalEvent<ContrabandPalletConsoleComponent, ContrabandPalletAppraiseMessage>(OnPalletAppraise);
        SubscribeLocalEvent<ContrabandPalletConsoleComponent, BoundUIOpenedEvent>(OnPalletUIOpen);
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

    private void OnPalletUIOpen(EntityUid uid, ContrabandPalletConsoleComponent component, BoundUIOpenedEvent args)
    {
        var player = args.Actor;

        if (player == null)
            return;

        UpdatePalletConsoleInterface(uid, component);
    }

    /// <summary>
    /// Ok so this is just the same thing as opening the UI, its a refresh button.
    /// I know this would probably feel better if it were like predicted and dynamic as pallet contents change
    /// However.
    /// I dont want it to explode if cargo uses a conveyor to move 8000 pineapple slices or whatever, they are
    /// known for their entity spam i wouldnt put it past them
    /// </summary>

    private void OnPalletAppraise(EntityUid uid, ContrabandPalletConsoleComponent component, ContrabandPalletAppraiseMessage args)
    {
        var player = args.Actor;

        if (player == null)
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

    private void SellPallets(EntityUid gridUid, ContrabandPalletConsoleComponent component, EntityUid? station, out int amount)
    {
        station ??= _station.GetOwningStation(gridUid);
        GetPalletGoods(gridUid, component, out var toSell, out amount);

        Log.Debug($"{component.Faction} sold {toSell.Count} contraband items for {amount}");

        if (station != null)
        {
            var ev = new EntitySoldEvent(toSell, gridUid);
            RaiseLocalEvent(ref ev);
        }

        foreach (var ent in toSell)
        {
            Del(ent);
        }
    }

    private void GetPalletGoods(EntityUid gridUid, ContrabandPalletConsoleComponent console, out HashSet<EntityUid> toSell, out int amount)
    {
        amount = 0;
        toSell = new HashSet<EntityUid>();

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

                if (TryComp<ContrabandComponent>(ent, out var comp))
                {
                    if (!comp.TurnInValues.ContainsKey(console.RewardType))
                        continue;

                    toSell.Add(ent);
                    var value = comp.TurnInValues[console.RewardType];
                    if (value <= 0)
                        continue;
                    amount += value;
                }
            }
        }
    }

    private bool CanSell(EntityUid uid, TransformComponent xform)
    {
        if (_mobQuery.HasComponent(uid))
        {
            if (_mobQuery.GetComponent(uid).CurrentState == MobState.Dead) // Allow selling alive prisoners
            {
                return false;
            }
            return true;
        }

        // Recursively check for mobs at any point.
        var children = xform.ChildEnumerator;
        while (children.MoveNext(out var child))
        {
            if (!CanSell(child, _xformQuery.GetComponent(child)))
                return false;
        }
        // Look for blacklisted items and stop the selling of the container.
        if (_blacklistQuery.HasComponent(uid))
        {
            return false;
        }
        return true;
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
}
