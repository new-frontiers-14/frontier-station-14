using System.Linq;
using Content.Server._NF.Market.Components;
using Content.Server._NF.Market.Extensions;
using Content.Server.Cargo.Systems;
using Content.Server.Power.Components;
using Content.Server.Storage.Components;
using Content.Shared._NF.Market;
using Content.Shared._NF.Market.BUI;
using Content.Shared._NF.Market.Events;
using Content.Shared.Bank.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Stacks;
using Content.Shared.Storage;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.Market.Systems;

public sealed partial class MarketSystem
{
    private void InitializeConsole()
    {
        SubscribeLocalEvent<EntitySoldEvent>(OnEntitySoldEvent);
        SubscribeLocalEvent<MarketConsoleComponent, BoundUIOpenedEvent>(OnConsoleUiOpened);
        SubscribeLocalEvent<MarketConsoleComponent, MarketConsoleCartMessage>(OnCartMessage);
        SubscribeLocalEvent<MarketConsoleComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnPowerChanged(EntityUid uid, MarketConsoleComponent component, ref PowerChangedEvent args)
    {
        if (args.Powered)
            return;
        _ui.CloseUi(uid, MarketConsoleUiKey.Default);
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
            if (_entityManager.TryGetComponent<StorageComponent>(sold, out var storageComponent))
                UpsertStorage(marketDataComponent, storageComponent);
            else if (_entityManager.TryGetComponent<EntityStorageComponent>(sold, out var entityStorageComponent))
                UpsertEntityStorage(marketDataComponent, entityStorageComponent);
            else if (_entityManager.TryGetComponent<ItemSlotsComponent>(sold, out var itemSlotsComponent))
                UpsertItemSlots(marketDataComponent, itemSlotsComponent);

            UpsertMetadata(marketDataComponent, sold);
        }
    }

    private void UpsertMetadata(MarketDataComponent marketDataComponent, EntityUid sold)
    {
        // Get the MetaDataComponent from the sold entity
        if (!_entityManager.TryGetComponent<MetaDataComponent>(sold, out var metaDataComponent))
            return;

        // Get the prototype ID of the sold entity
        if (metaDataComponent.EntityPrototype == null)
            return;

        var count = 1;
        var entityPrototype = metaDataComponent.EntityPrototype;
        string? stackPrototypeId = null;

        // Get amount of items in the stack if it's a stackable item.
        // If it's a stackable item, get the singular item id instead.
        if (_entityManager.TryGetComponent<StackComponent>(sold, out var stackComponent))
        {
            count = stackComponent.Count;
            stackPrototypeId = stackComponent.StackTypeId;
            var singularId = _prototypeManager.Index<StackPrototype>(stackComponent.StackTypeId).Spawn.Id;
            _prototypeManager.TryIndex(singularId, out entityPrototype);
        }

        // If this is null, probably couldnt find the stack type id.
        if (entityPrototype == null)
            return;

        var estimatedPrice = _pricingSystem.GetEstimatedPrice(entityPrototype);

        // Increase the count in the MarketData for this entity
        // Assuming the quantity to increase is 1 for each sold entity
        marketDataComponent.MarketDataList.Upsert(entityPrototype, count, estimatedPrice, stackPrototypeId);
    }

    /// <summary>
    /// Recursively updates or inserts market data for entities contained within an EntityStorageComponent.
    /// </summary>
    /// <param name="marketDataComponent">The MarketDataComponent to update.</param>
    /// <param name="entityStorageComponent">The EntityStorageComponent containing entities to process.</param>
    private void UpsertEntityStorage(MarketDataComponent marketDataComponent, EntityStorageComponent entityStorageComponent)
    {
        foreach (var entityUid in entityStorageComponent.Contents.ContainedEntities)
        {
            if (_entityManager.TryGetComponent<StorageComponent>(entityUid, out var storageComponent))
            {
                UpsertStorage(marketDataComponent, storageComponent);
            }
            else if (_entityManager.TryGetComponent<EntityStorageComponent>(entityUid, out var nestedEntityStorageComponent))
            {
                UpsertEntityStorage(marketDataComponent, nestedEntityStorageComponent);
            }
            UpsertMetadata(marketDataComponent, entityUid);
        }
    }

    /// <summary>
    /// Recursively updates or inserts market data for entities contained within an ItemSlotsComponent.
    /// </summary>
    /// <param name="marketDataComponent">The MarketDataComponent to update.</param>
    /// <param name="itemSlotsComponent">The ItemSlotsComponent containing item slots to process.</param>
    private void UpsertItemSlots(MarketDataComponent marketDataComponent, ItemSlotsComponent itemSlotsComponent)
    {
        foreach (var slot in itemSlotsComponent.Slots.Values)
        {
            if (slot.Item is not { Valid: true } entityUid)
                continue;

            if (_entityManager.TryGetComponent<StorageComponent>(entityUid, out var storageComponent))
            {
                UpsertStorage(marketDataComponent, storageComponent);
            }
            else if (_entityManager.TryGetComponent<EntityStorageComponent>(entityUid, out var entityStorageComponent))
            {
                UpsertEntityStorage(marketDataComponent, entityStorageComponent);
            }
            UpsertMetadata(marketDataComponent, entityUid);
        }
    }

    /// <summary>
    /// Recursively checks the contents of the storage.
    /// </summary>
    /// <param name="marketDataComponent"></param>
    /// <param name="storageComponent"></param>
    private void UpsertStorage(MarketDataComponent marketDataComponent, StorageComponent storageComponent)
    {
        foreach (var entityUid in storageComponent.Container.ContainedEntities.ToArray())
        {
            if (_entityManager.TryGetComponent<StorageComponent>(entityUid, out var comp))
                UpsertStorage(marketDataComponent, comp);

            UpsertMetadata(marketDataComponent, entityUid);
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
            component);
    }

    private void RefreshState(
        EntityUid consoleUid,
        int balance,
        float marketMultiplier,
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
        var cartBalance = MarketDataExtensions.GetMarketValue(cartData, marketMultiplier);

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
            true, // TODO add enable/disable functionality
            component.TransactionCost
        );
        _ui.SetUiState(consoleUid, MarketConsoleUiKey.Default, newState);
    }
}
