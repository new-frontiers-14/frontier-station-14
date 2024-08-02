using System.Linq;
using Content.Server._NF.Market.Components;
using Content.Server._NF.Market.Extensions;
using Content.Server.Cargo.Systems;
using Content.Shared._NF.Market;
using Content.Shared._NF.Market.BUI;
using Content.Shared._NF.Market.Events;
using Content.Shared.Bank.Components;
using Content.Shared.Stacks;
using Content.Shared.Whitelist;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.Market.Systems;

public sealed partial class MarketSystem : SharedMarketSystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly PricingSystem _pricingSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EntitySoldEvent>(OnEntitySoldEvent);
        SubscribeLocalEvent<MarketConsoleComponent, BoundUIOpenedEvent>(OnConsoleUiOpened);
        SubscribeLocalEvent<MarketConsoleComponent, MarketConsoleCartMessage>(OnCartMessage);
        InitializeCrateMachine();
    }

    /// <summary>
    /// This event signifies that something has been sold at a cargo pallet.
    /// </summary>
    /// <param name="entitySoldEvent">The details of the event</param>
    private void OnEntitySoldEvent(ref EntitySoldEvent entitySoldEvent)
    {
        var marketDataComponent = _entityManager.EnsureComponent<MarketDataComponent>(entitySoldEvent.Station);
        foreach (var sold in entitySoldEvent.Sold)
        {
            // Get the MetaDataComponent from the sold entity
            if (!_entityManager.TryGetComponent<MetaDataComponent>(sold, out var metaData))
                continue;

            // Get the prototype ID of the sold entity
            if (metaData.EntityPrototype == null)
                continue;

            var count = 1;

            // Get amount of items in the stack if it's a stackable item.
            if (_entityManager.TryGetComponent<StackComponent>(sold, out var stackComponent))
            {
                count = stackComponent.Count;
            }

            var estimatedPrice = _pricingSystem.GetEstimatedPrice(metaData.EntityPrototype) / count;

            // Increase the count in the MarketData for this entity
            // Assuming the quantity to increase is 1 for each sold entity
            marketDataComponent.MarketDataList.Upsert(metaData.EntityPrototype, count, estimatedPrice);
        }
    }

    /// <summary>
    /// Occurs whenever something is added to the cart.
    /// If args.Amount is too high it will automatically be clamped to the maximum amount possible.
    /// This also prevents desync if there are two different consoles.
    /// </summary>
    /// <param name="consoleUid">The uuid of the console where it was added.</param>
    /// <param name="consoleComponent">The console component</param>
    /// <param name="args">The arguments for the cart event</param>
    private void OnCartMessage(
        EntityUid consoleUid,
        MarketConsoleComponent consoleComponent,
        ref MarketConsoleCartMessage args
    )
    {
        if (args.Actor is not { Valid: true } player)
            return;
        if (!TryComp<BankAccountComponent>(player, out var bank))
            return;
        var marketMultiplier = 1.0f;
        if (TryComp<MarketModifierComponent>(consoleUid, out var priceMod))
        {
            marketMultiplier = priceMod.Mod;
        }

        var gridUid = Transform(consoleUid).GridUid!.Value;

        // Try to get the EntityPrototype that matches marketData.Prototype
        if (!_prototypeManager.TryIndex<EntityPrototype>(args.ItemPrototype!, out var prototype))
        {
            return; // Skip this iteration if the prototype was not found
        }
        var estimatedPrice = _pricingSystem.GetEstimatedPrice(prototype);

        var marketData = _entityManager.EnsureComponent<MarketDataComponent>(gridUid).MarketDataList;
        var maxQuantityToWithdraw = marketData.GetMaxQuantityToWithdraw(prototype);
        var toWithdraw = args.Amount;
        if (args.Amount > maxQuantityToWithdraw)
        {
            toWithdraw = maxQuantityToWithdraw;
        }

        marketData.Upsert(prototype, -toWithdraw, estimatedPrice);
        consoleComponent.CartDataList.Upsert(prototype, toWithdraw, estimatedPrice);

        RefreshState(
            consoleUid,
            bank.Balance,
            marketMultiplier,
            MarketConsoleUiKey.Default,
            consoleComponent
        );
    }

    private void OnConsoleUiOpened(EntityUid uid, MarketConsoleComponent component, BoundUIOpenedEvent args)
    {
        if (args.Actor is not { Valid: true } player)
            return;
        if (!TryComp<BankAccountComponent>(player, out var bank))
            return;
        var marketMultiplier = 1.0f;
        if (TryComp<MarketModifierComponent>(uid, out var priceMod))
        {
            marketMultiplier = priceMod.Mod;
        }

        RefreshState(uid,
            bank.Balance,
            marketMultiplier,
            MarketConsoleUiKey.Default,
            component);
    }

    private static int GetMarketSelectionValue(List<MarketData> dataList, float marketModifier)
    {
        var cartBalance = 0;

        if (!(dataList.Count >= 1))
            return cartBalance;

        foreach (var marketData in dataList)
        {
            cartBalance += (int) Math.Round(marketData.Price * marketData.Quantity * marketModifier);
        }

        return cartBalance;
    }

    private void RefreshState(
        EntityUid consoleUid,
        int balance,
        float marketMultiplier,
        MarketConsoleUiKey uiKey,
        MarketConsoleComponent? component
    )
    {
        if (!Resolve(consoleUid, ref component))
            return;

        // Ensures that when this console is no longer attached to a grid and is powered somehow, it won't work.
        if (Transform(consoleUid).GridUid == null)
            return;

        // Get the market data for this grid.
        var consoleGridUid = Transform(consoleUid).GridUid!.Value;
        var cartData = component.CartDataList;
        var marketData = _entityManager.EnsureComponent<MarketDataComponent>(consoleGridUid).MarketDataList;
        var cartBalance = GetMarketSelectionValue(cartData, marketMultiplier);

        if (component.Whitelist != null)
        {
            marketData = marketData
                .Where(item => _prototypeManager.TryIndex(item.Prototype, out var entityPrototype, false) &&
                    _whitelistSystem.IsPrototypeWhitelistPass(component.Whitelist!, entityPrototype))
                .ToList();
        }

        if (component.Blacklist != null)
        {
            marketData = marketData
                .Where(item => _prototypeManager.TryIndex(item.Prototype, out var entityPrototype, false) &&
                               _whitelistSystem.IsPrototypeBlacklistFail(component.Blacklist!, entityPrototype))
                .ToList();
        }


        var newState = new MarketConsoleInterfaceState(
            balance,
            marketMultiplier,
            marketData,
            cartData,
            cartBalance,
            true // TODO add enable/disable functionality
        );
        _ui.SetUiState(consoleUid, uiKey, newState);
    }
}
