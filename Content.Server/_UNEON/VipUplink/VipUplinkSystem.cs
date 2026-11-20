using System.Linq;
using Content.Server.CartridgeLoader;
using Content.Server._NF.Bank;
using Content.Server._UNEON.Subscriptions;
using Content.Shared._UNEON.VipUplink;
using Content.Shared.CartridgeLoader;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.PDA;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._UNEON.VipUplink;

public sealed class VipUplinkSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem _cartridges = default!;
    [Dependency] private readonly UneonSubscriptionsSystem _subscriptions = default!;
    [Dependency] private readonly BankSystem _bank = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly MobStateSystem _mobs = default!;
    [Dependency] private readonly SharedPopupSystem _popups = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly ISharedPlayerManager _players = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    private readonly Dictionary<EntityUid, TimeSpan> _lastPurchase = new();
    private TimeSpan _nextRefresh;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PdaComponent, MapInitEvent>(OnPdaInit, after: new[] { typeof(CartridgeLoaderSystem) });
        SubscribeLocalEvent<VipUplinkCartridgeComponent, CartridgeUiReadyEvent>(OnReady);
        SubscribeLocalEvent<VipUplinkCartridgeComponent, CartridgeProgramAccessAttemptEvent>(OnAccess);
        // The loader relays this as its base event, as with other cartridge applications.
        SubscribeLocalEvent<VipUplinkCartridgeComponent, CartridgeMessageEvent>(OnMessage);
    }

    private void OnAccess(EntityUid uid, VipUplinkCartridgeComponent component, CartridgeProgramAccessAttemptEvent args)
    {
        if (!_players.TryGetSessionByEntity(args.Actor, out var session) || _subscriptions.GetTier(session.UserId) <= 0)
            args.Cancel();
    }

    private void OnPdaInit(EntityUid uid, PdaComponent component, MapInitEvent args)
    {
        if (!TryComp<CartridgeLoaderComponent>(uid, out var loader) ||
            _cartridges.HasProgram<VipUplinkCartridgeComponent>(uid))
            return;
        // Reserve an additional slot: existing PDA programs keep all their original disk space.
        loader.DiskSpace++;
        _cartridges.InstallProgram(uid, "UneonVipUplinkCartridge", deinstallable: false, loader: loader);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        if (_timing.CurTime < _nextRefresh)
            return;
        _nextRefresh = _timing.CurTime + TimeSpan.FromSeconds(1);
        foreach (var session in _players.Sessions)
        {
            if (session.AttachedEntity is not { } actor)
                continue;
            var access = EnsureComp<VipUplinkAccessComponent>(actor);
            var tier = _subscriptions.GetTier(session.UserId);
            if (access.Tier != tier)
            {
                access.Tier = tier;
                Dirty(actor, access);
            }
        }
        var query = EntityQueryEnumerator<VipUplinkCartridgeComponent, CartridgeComponent>();
        while (query.MoveNext(out var uid, out _, out var cartridge))
        {
            if (cartridge.LoaderUid is { } loader &&
                TryComp<CartridgeLoaderComponent>(loader, out var comp) && comp.ActiveProgram == uid)
                UpdateViewers(loader, comp);
        }
        foreach (var actor in _lastPurchase.Keys.Where(a => Deleted(a) ||
                     _timing.CurTime - _lastPurchase[a] > TimeSpan.FromMinutes(1)).ToArray())
            _lastPurchase.Remove(actor);
    }

    private void OnReady(EntityUid uid, VipUplinkCartridgeComponent component, CartridgeUiReadyEvent args)
    {
        if (TryComp<CartridgeLoaderComponent>(args.Loader, out var loader) && loader.ActiveProgram == uid)
            UpdateViewers(args.Loader, loader);
    }

    private void UpdateViewers(EntityUid loader, CartridgeLoaderComponent component)
    {
        foreach (var actor in _ui.GetActors(loader, component.UiKey))
        {
            var products = new List<VipUplinkListing>();
            var tier = 0;
            var account = "";
            if (_players.TryGetSessionByEntity(actor, out var session))
            {
                tier = _subscriptions.GetTier(session.UserId);
                account = session.UserId.ToString();
            }
            // No other subscriber ever receives another account's personal suit in this response.
            if (tier > 0)
            {
                foreach (var product in _prototypes.EnumeratePrototypes<VipUplinkProductPrototype>().OrderBy(p => p.ID))
                {
                    if (product.IsAvailable(tier, account))
                        products.Add(new VipUplinkListing(product.ID, product.Name, product.Price));
                }
            }
            _bank.TryGetBalance(actor, out var balance);
            _ui.ServerSendUiMessage(loader, component.UiKey,
                new VipUplinkStateMessage(new VipUplinkState(tier > 0, balance, products)), actor);
        }
    }

    private void OnMessage(EntityUid uid, VipUplinkCartridgeComponent component, CartridgeMessageEvent args)
    {
        if (args is not VipUplinkBuyMessage message)
            return;
        var loader = GetEntity(args.LoaderUid);
        var actor = args.Actor;
        if (!TryComp<CartridgeLoaderComponent>(loader, out var loaderComp) || loaderComp.ActiveProgram != uid ||
            !TryComp<CartridgeComponent>(uid, out var cartridge) || cartridge.LoaderUid != loader ||
            !_ui.GetActors(loader, loaderComp.UiKey).Contains(actor) ||
            !_players.TryGetSessionByEntity(actor, out var session) || !_mobs.IsAlive(actor) ||
            !_interaction.InRangeUnobstructed(actor, loader) || Transform(actor).MapID == MapId.Nullspace)
            return;
        if (_lastPurchase.TryGetValue(actor, out var last) && _timing.CurTime - last < TimeSpan.FromSeconds(1))
            return;
        _lastPurchase[actor] = _timing.CurTime;
        if (string.IsNullOrEmpty(message.ProductId) || message.ProductId.Length > 128 ||
            !_prototypes.TryIndex<VipUplinkProductPrototype>(message.ProductId, out var product) ||
            !product.IsAvailable(_subscriptions.GetTier(session.UserId), session.UserId.ToString()) ||
            !_bank.TryGetBalance(actor, out var balance) || balance < product.Price)
        {
            _popups.PopupEntity(Loc.GetString("uneon-vip-failed"), actor, actor);
            return;
        }

        // Stage the item outside the world before charging. A failed prototype spawn never costs money.
        EntityUid item;
        try { item = Spawn(product.Product); }
        catch (Exception e)
        {
            Log.Error($"VIP uplink could not spawn product {product.ID}: {e.GetType().Name}");
            _popups.PopupEntity(Loc.GetString("uneon-vip-failed"), actor, actor);
            return;
        }
        if (!_bank.TryBankWithdraw(actor, product.Price))
        {
            QueueDel(item);
            _popups.PopupEntity(Loc.GetString("uneon-vip-failed"), actor, actor);
            return;
        }
        // Use the actor's coordinates, never a client supplied position or the PDA's ID card owner.
        _transform.SetCoordinates(item, Transform(actor).Coordinates);
        _hands.TryPickupAnyHand(actor, item); // Failed pickup leaves the purchase beside the character.
        Log.Info($"VIP uplink: {session.UserId} bought {product.ID} for {product.Price}");
        _popups.PopupEntity(Loc.GetString("uneon-vip-success"), actor, actor);
        UpdateViewers(loader, loaderComp);
    }
}
