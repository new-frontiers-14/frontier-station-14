using System.Linq;
using Content.Server._NF.Market.Components;
using Content.Server._NF.Market.Extensions;
using Content.Server._NF.SectorServices;
using Content.Server.Cargo.Systems;
using Content.Server.Power.Components;
using Content.Server.Storage.Components;
using Content.Shared._NF.Market;
using Content.Shared._NF.Market.BUI;
using Content.Shared._NF.Market.Events;
using Content.Shared.Bank.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Prototypes;
using Content.Shared.Stacks;
using Content.Shared.Storage;
using Content.Shared.Whitelist;
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
        var station = _station.GetOwningStation(entitySoldEvent.Grid);
        if (station is null ||
            !_entityManager.TryGetComponent<CargoMarketDataComponent>(station, out var market))
        {
            return;
        }

        foreach (var sold in entitySoldEvent.Sold)
        {
            if (_entityManager.TryGetComponent<StorageComponent>(sold, out var storageComponent))
                UpsertStorage(market, storageComponent);
            else if (_entityManager.TryGetComponent<EntityStorageComponent>(sold, out var entityStorageComponent))
                UpsertEntityStorage(market, entityStorageComponent);
            else if (_entityManager.TryGetComponent<ItemSlotsComponent>(sold, out var itemSlotsComponent))
                UpsertItemSlots(market, itemSlotsComponent);

            UpsertMetadata(market, sold);
        }
    }

    private void UpsertMetadata(CargoMarketDataComponent marketDataComponent, EntityUid sold)
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

        // Check whitelist/blacklist for particular prototype
        if (_whitelistSystem.IsPrototypeWhitelistPassOrNull(marketDataComponent.Whitelist, entityPrototype) &&
            _whitelistSystem.IsPrototypeBlacklistFailOrNull(marketDataComponent.Blacklist, entityPrototype) ||
            _protoIdWhitelist.IsPrototypeWhitelistPassOrNull(marketDataComponent.OverrideList, entityPrototype))
        {
            var estimatedPrice = _pricingSystem.GetPrice(sold) / count;

            // Increase the count in the MarketData for this entity
            // Assuming the quantity to increase is 1 for each sold entity
            marketDataComponent.MarketDataList.Upsert(entityPrototype.ID, count, estimatedPrice, stackPrototypeId);
        }
    }

    /// <summary>
    /// Recursively updates or inserts market data for entities contained within an EntityStorageComponent.
    /// </summary>
    /// <param name="marketDataComponent">The MarketDataComponent to update.</param>
    /// <param name="entityStorageComponent">The EntityStorageComponent containing entities to process.</param>
    private void UpsertEntityStorage(CargoMarketDataComponent marketDataComponent, EntityStorageComponent entityStorageComponent)
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
    private void UpsertItemSlots(CargoMarketDataComponent marketDataComponent, ItemSlotsComponent itemSlotsComponent)
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
    private void UpsertStorage(CargoMarketDataComponent marketDataComponent, StorageComponent storageComponent)
    {
        foreach (var entityUid in storageComponent.Container.ContainedEntities.ToArray())
        {
            if (_entityManager.TryGetComponent<StorageComponent>(entityUid, out var comp))
                UpsertStorage(marketDataComponent, comp);

            UpsertMetadata(marketDataComponent, entityUid);
        }
    }

    /// <summary>
    /// Calculates the total number of entities in the market data list, taking into account the maximum stack count for stackable items.
    /// </summary>
    /// <param name="marketDataList">The list of market data to calculate the total entity count from.</param>
    /// <returns>The total number of entities in the market data list.</returns>
    public int CalculateEntityAmount(List<MarketData> marketDataList)
    {
        var count = 0;

        foreach (var data in marketDataList)
        {
            if (data.StackPrototype != null && _prototypeManager.TryIndex(data.StackPrototype, out var stackPrototype))
            {
                var maxStackCount = stackPrototype.MaxCount;
                if (maxStackCount != null)
                    count += (int) Math.Ceiling((double) data.Quantity / int.Max(1, maxStackCount.Value)); // Ensure denominator is positive
                else
                    count += 1;
            }
            else
            {
                count += 1;
            }
        }

        return count;
    }

    /// <summary>
    /// Calculates the amount of items that can fit within an entity's worth of space for a given item type.
    /// </summary>
    /// <param name="data">The item type to calculate.</param>
    /// <returns>The number of items that can fit within an entity's worth of space. Null if infinite.</returns>
    public int? GetAmountPerEntitySpace(MarketData data)
    {
        if (data.StackPrototype != null && _prototypeManager.TryIndex(data.StackPrototype, out var stackPrototype))
        {
            var maxStackCount = stackPrototype.MaxCount;
            if (maxStackCount != null)
                return int.Max(1, maxStackCount.Value); // Ensure amount is positive.
            else
                return null; // Infinity.
        }
        else
        {
            return 1;
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

        // Try to get the EntityPrototype that matches marketData.Prototype
        if (!_prototypeManager.TryIndex<EntityPrototype>(args.ItemPrototype!, out var prototype))
        {
            return; // Skip this iteration if the prototype was not found
        }

        // No data set for market data, can't update cart, no data.
        var stationUid = _station.GetOwningStation(consoleUid);
        if (!TryComp<CargoMarketDataComponent>(stationUid, out var market))
            return;

        var marketData = market.MarketDataList;
        if (args.RemoveFromCart)
        {
            consoleComponent.CartDataList.Move(marketData, prototype.ID);
        }
        else
        {
            var maxQuantityToWithdraw = marketData.GetMaxQuantityToWithdraw(prototype);
            var toWithdraw = args.Amount;
            if (args.Amount > maxQuantityToWithdraw)
                toWithdraw = maxQuantityToWithdraw;

            var existing = FindMarketDataByPrototype(marketData, args.ItemPrototype!);
            if (existing == null)
                return;

            // Calculate maximum we can fit.
            var entityAmount = CalculateEntityAmount(consoleComponent.CartDataList);
            var amountPerEntity = GetAmountPerEntitySpace(existing);
            int amountLeft;
            if (amountPerEntity == null)
            {
                amountLeft = int.MaxValue; // Infinite stack, infinite space.
            }
            else
            {
                amountLeft = (30 - entityAmount) * amountPerEntity.Value;

                var existingCart = FindMarketDataByPrototype(consoleComponent.CartDataList, args.ItemPrototype!);
                if (existingCart != null)
                {
                    // Find if there's a partially filled entity in the cart.
                    var quantityMod = existingCart.Quantity % amountPerEntity.Value;
                    if (quantityMod != 0)
                    {
                        amountLeft += amountPerEntity.Value - quantityMod;
                    }
                }
                amountLeft = int.Max(0, amountLeft); // If we're over the limit as-is, don't move anything.
            }

            toWithdraw = int.Min(toWithdraw, amountLeft);

            marketData.Upsert(existing.Prototype, -toWithdraw, existing.Price, existing.StackPrototype);
            consoleComponent.CartDataList.Upsert(existing.Prototype, toWithdraw, existing.Price, existing.StackPrototype);
        }

        RefreshState(
            consoleUid,
            bank.Balance,
            marketMultiplier,
            consoleComponent
        );
    }

    /// <summary>
    /// Finds a MarketData item in the list that has the same prototype.
    /// </summary>
    /// <param name="marketDataList">The list of market data to search in.</param>
    /// <param name="prototypeId">The prototype ID to search for.</param>
    /// <returns>The MarketData item with the matching prototype, or null if not found.</returns>
    public MarketData? FindMarketDataByPrototype(List<MarketData> marketDataList, string prototypeId)
    {
        foreach (var marketData in marketDataList)
        {
            if (marketData.Prototype == prototypeId)
            {
                return marketData;
            }
        }
        return null;
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
        var cartData = component.CartDataList;
        var marketData = new List<MarketData>();

        // Get station and the market data attached to it.
        var consoleStationUid = _station.GetOwningStation(consoleUid);
        if (TryComp<CargoMarketDataComponent>(consoleStationUid, out var market))
        {
            marketData = market.MarketDataList;
        }
        var cartBalance = MarketDataExtensions.GetMarketValue(cartData, marketMultiplier);

        var newState = new MarketConsoleInterfaceState(
            balance,
            marketMultiplier,
            marketData,
            cartData,
            cartBalance,
            true, // TODO add enable/disable functionality
            component.TransactionCost,
            CalculateEntityAmount(cartData)
        );
        _ui.SetUiState(consoleUid, MarketConsoleUiKey.Default, newState);
    }
}
