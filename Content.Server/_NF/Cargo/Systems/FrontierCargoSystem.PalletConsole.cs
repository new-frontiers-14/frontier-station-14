using Content.Shared._NF.Cargo;
using Content.Shared.Mobs;

namespace Content.Server._NF.Cargo.Systems;

using Components;
using Content.Shared._NF.Cargo.Events;
using Content.Shared.Cargo;
using Content.Shared.Cargo.BUI;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Events;
using Content.Server.Cargo.Components;
using Content.Shared._NF.Bank.Components;
using Shared.Stacks;
using Content.Shared.GameTicking;
using Robust.Shared.Audio;

/*
 * Handles cargo shuttle / trade mechanics.
 */
public sealed partial class FrontierCargoSystem
{
    private static readonly SoundPathSpecifier ApproveSound = new("/Audio/Effects/Cargo/ping.ogg");

    private void InitializeShuttle()
    {
        SubscribeLocalEvent<FrontierTradeStationComponent, GridSplitEvent>(OnTradeSplit);
        SubscribeLocalEvent<FrontierCargoPalletConsoleComponent, FrontierCargoPalletSellMessage>(OnPalletSale);
        SubscribeLocalEvent<FrontierCargoPalletConsoleComponent, FrontierCargoPalletAppraiseMessage>(OnPalletAppraise);
        SubscribeLocalEvent<FrontierCargoPalletConsoleComponent, BoundUIOpenedEvent>(OnPalletUIOpen);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    #region Console

    private void UpdatePalletConsoleInterface(EntityUid uid)
    {
        var bui = _uiSystem.GetUi(uid, FrontierCargoPalletConsoleUiKey.Sale);
        if (Transform(uid).GridUid is not EntityUid gridUid)
        {
            _uiSystem.SetUiState(bui,
                new CargoPalletConsoleInterfaceState(0, 0, false));
            return;
        }

        GetPalletGoods(gridUid, out var toSell, out var amount);
        if (TryComp<MarketModifierComponent>(uid, out var priceMod))
        {
            amount *= priceMod.Mod;
        }

        _uiSystem.SetUiState(bui,
            new CargoPalletConsoleInterfaceState((int) amount, toSell.Count, true));
    }

    private void OnPalletUIOpen(EntityUid uid, FrontierCargoPalletConsoleComponent component, BoundUIOpenedEvent args)
    {
        var player = args.Session.AttachedEntity;

        if (player == null)
            return;

        UpdatePalletConsoleInterface(uid);
    }

    /// <summary>
    /// Ok so this is just the same thing as opening the UI, its a refresh button.
    /// I know this would probably feel better if it were like predicted and dynamic as pallet contents change
    /// However.
    /// I dont want it to explode if cargo uses a conveyor to move 8000 pineapple slices or whatever, they are
    /// known for their entity spam i wouldnt put it past them
    /// </summary>
    private void OnPalletAppraise(EntityUid uid, FrontierCargoPalletConsoleComponent component, FrontierCargoPalletAppraiseMessage args)
    {
        var player = args.Session.AttachedEntity;

        if (player == null)
            return;

        UpdatePalletConsoleInterface(uid);
    }

    /// <summary>
    /// Get the amount of space the cargo shuttle can fit for orders.
    /// </summary>
    private int GetCargoSpace(EntityUid gridUid)
    {
        var space = GetCargoPallets(gridUid).Count;
        return space;
    }

    private List<(EntityUid Entity, FrontierCargoPalletComponent Component, TransformComponent PalletXform)> GetCargoPallets(EntityUid gridUid)
    {
        _pads.Clear();
        var query = AllEntityQuery<FrontierCargoPalletComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var comp, out var compXform))
        {
            if (compXform.ParentUid != gridUid ||
                !compXform.Anchored)
            {
                continue;
            }

            _pads.Add((uid, comp, compXform));
        }

        return _pads;
    }

    #endregion

    private void OnTradeSplit(EntityUid uid, FrontierTradeStationComponent component, ref GridSplitEvent args)
    {
        // If the trade station gets bombed it's still a trade station.
        foreach (var gridUid in args.NewGrids)
        {
            EnsureComp<FrontierTradeStationComponent>(gridUid);
        }
    }

    private void OnPalletSale(EntityUid uid, FrontierCargoPalletConsoleComponent component, FrontierCargoPalletSellMessage args)
    {
        var player = args.Session.AttachedEntity;

        if (player == null)
            return;

        var bui = _uiSystem.GetUi(uid, FrontierCargoPalletConsoleUiKey.Sale);
        var xform = Transform(uid);

        if (xform.GridUid is not EntityUid gridUid)
        {
            _uiSystem.SetUiState(bui,
                new CargoPalletConsoleInterfaceState(0, 0, false));
            return;
        }

        if (!SellPallets(gridUid, null, out var price))
            return;

        if (TryComp<MarketModifierComponent>(uid, out var priceMod))
        {
            price *= priceMod.Mod;
        }
        var stackPrototype = _protoMan.Index<StackPrototype>(component.CashType);
        _stack.Spawn((int) price, stackPrototype, xform.Coordinates);
        _audio.PlayPvs(ApproveSound, uid);
        UpdatePalletConsoleInterface(uid);
    }

    private bool SellPallets(EntityUid gridUid, EntityUid? station, out double amount)
    {
        station ??= _station.GetOwningStation(gridUid);
        GetPalletGoods(gridUid, out var toSell, out amount);

        Log.Debug($"Cargo sold {toSell.Count} entities for {amount}");

        if (toSell.Count == 0)
            return false;

        if (station != null)
        {
            var ev = new EntitySoldEvent(station.Value, toSell);
            RaiseLocalEvent(ref ev);
        }

        foreach (var ent in toSell)
        {
            Del(ent);
        }

        return true;
    }

    private void GetPalletGoods(EntityUid gridUid, out HashSet<EntityUid> toSell, out double amount)
    {
        amount = 0;
        toSell = new HashSet<EntityUid>();

        foreach (var (palletUid, _, _) in GetCargoPallets(gridUid))
        {
            // Containers should already get the sell price of their children so can skip those.
            _setEnts.Clear();

            _lookup.GetEntitiesIntersecting(palletUid, _setEnts,
                LookupFlags.Dynamic | LookupFlags.Sundries);

            foreach (var ent in _setEnts)
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

                var price = _pricing.GetPrice(ent);
                if (price == 0)
                    continue;
                toSell.Add(ent);
                amount += price;
            }
        }
    }

    private bool CanSell(EntityUid uid, TransformComponent xform)
    {
        if (_mobQuery.HasComponent(uid))
        {
            if (_mobQuery.GetComponent(uid).CurrentState == MobState.Alive)
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

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        Reset();
    }
}

/// <summary>
/// Event broadcast raised by-ref before it is sold and
/// deleted but after the price has been calculated.
/// </summary>
[ByRefEvent]
public readonly record struct EntitySoldEvent(EntityUid Station, HashSet<EntityUid> Sold);
