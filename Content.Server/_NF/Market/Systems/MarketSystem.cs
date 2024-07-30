using System.Linq;
using Content.Server._NF.Market.Components;
using Content.Server.Bank;
using Content.Server.Cargo.Systems;
using Content.Server.Station.Systems;
using Content.Shared._NF.Market;
using Content.Shared._NF.Market.BUI;
using Content.Shared._NF.Market.Events;
using Content.Shared.Bank.Components;
using Content.Shared.Cargo.Components;
using Content.Shared.Stacks;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.Market.Systems;

public sealed partial class MarketSystem : SharedMarketSystem
{
    [Dependency] private readonly BankSystem _bank = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private readonly List<MarketData> _marketDataList = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EntitySoldEvent>(OnEntitySoldEvent);
        SubscribeLocalEvent<MarketConsoleComponent, BoundUIOpenedEvent>(OnConsoleUiOpened);
        SubscribeLocalEvent<MarketConsoleComponent, CrateMachineCartMessage>(OnCartMessage);
        InitializeCrateMachine();
    }

    private void OnEntitySoldEvent(ref EntitySoldEvent ev)
    {
        foreach (var sold in ev.Sold)
        {
            // Get the MetaDataComponent from the sold entity
            if (_entityManager.TryGetComponent<MetaDataComponent>(sold, out var metaData))
            {
                // Get the prototype ID of the sold entity
                var entityPrototypeId = metaData.EntityPrototype?.ID;

                if (entityPrototypeId == null)
                    continue; // Skip items without prototype id

                var count = 1;

                // Get amount of items in the stack if it's a stackable item.
                if (_entityManager.TryGetComponent<StackComponent>(sold, out var stackComponent))
                {
                    count = stackComponent.Count;
                }

                // Increase the count in the MarketData for this entity
                // Assuming the quantity to increase is 1 for each sold entity
                TryUpdateMarketData(entityPrototypeId, count, ev.Station);
            }
        }
    }

    private void OnCartMessage(
        EntityUid consoleUid,
        MarketConsoleComponent consoleComponent,
        ref CrateMachineCartMessage args
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

        if (!(_station.GetOwningStation(consoleUid) is { Valid: true } station))
            return;

        if (TryUpdateMarketData(args.ItemPrototype!, args.Amount, station))
        {
            var stationNetEntity = GetNetEntity(station);
            var itemProto = args.ItemPrototype;
            // Find the MarketData for the given EntityPrototype
            var marketData =
                consoleComponent.CartData.FirstOrDefault(md =>
                    md.Prototype == itemProto && md.StationUid == stationNetEntity);
            if (marketData != null && (marketData.Quantity - args.Amount) >= 0)
            {
                // If it exists, change the count
                marketData.Quantity -= args.Amount;
                if (marketData.Quantity <= 0)
                {
                    consoleComponent.CartData.Remove(marketData);
                }
            }
            else if (args.Amount < 0)
            {
                consoleComponent.CartData.Add(new MarketData(args.ItemPrototype!, args.Amount, stationNetEntity));
            }
        }

        RefreshState(consoleUid,
            bank.Balance,
            marketMultiplier,
            _marketDataList,
            consoleComponent.CartData,
            MarketConsoleUiKey.Default,
            consoleComponent);
    }

    /// <summary>
    /// Updates the market data list or adds it new if it doesnt exist in there yet.
    /// </summary>
    /// <param name="entityPrototypeId"></param>
    /// <param name="increaseAmount"></param>
    public bool TryUpdateMarketData(string entityPrototypeId, int increaseAmount, EntityUid station)
    {
        var stationNetEntity = GetNetEntity(station);
        // Find the MarketData for the given EntityPrototype
        var marketData =
            _marketDataList.FirstOrDefault(md =>
                md.Prototype == entityPrototypeId && md.StationUid == stationNetEntity);

        if (marketData != null && (marketData.Quantity + increaseAmount) >= 0)
        {
            // If it exists, change the count
            marketData.Quantity += increaseAmount;
            if (marketData.Quantity <= 0)
            {
                _marketDataList.Remove(marketData);
            }

            return true;
        }

        // If it doesn't exist, create a new MarketData and add it to the list
        if (increaseAmount > 0)
        {
            _marketDataList.Add(new MarketData(entityPrototypeId, increaseAmount, stationNetEntity));
            return true;
        }

        return false;
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
            _marketDataList,
            component.CartData,
            MarketConsoleUiKey.Default,
            component);
    }

    private int GetMarketSelectionValue(List<MarketData> dataList, float marketModifier)
    {
        var cartBalance = 0;

        if (!(dataList.Count >= 1))
            return cartBalance;

        foreach (var marketData in dataList)
        {
            // Try to get the EntityPrototype that matches marketData.Prototype
            if (!_prototypeManager.TryIndex<EntityPrototype>(marketData.Prototype, out var prototype))
            {
                continue; // Skip this iteration if the prototype was not found
            }

            var price = 0f;
            if (prototype.TryGetComponent<StaticPriceComponent>(out var staticPrice))
            {
                price = (float) (staticPrice.Price * marketModifier);
            }

            var subTotal = (int) Math.Round(price * marketData.Quantity);
            cartBalance += subTotal;
        }

        return cartBalance;
    }

    private void RefreshState(
        EntityUid uid,
        int balance,
        float marketMultiplier,
        List<MarketData> data,
        List<MarketData> cartData,
        MarketConsoleUiKey uiKey,
        MarketConsoleComponent? component
    )
    {
        if (!Resolve(uid, ref component))
            return;

        var cartBalance = GetMarketSelectionValue(cartData, marketMultiplier);

        var newState = new MarketConsoleInterfaceState(
            balance,
            marketMultiplier,
            data,
            cartData,
            cartBalance,
            true // TODO add enable/disable functionality
        );
        _ui.SetUiState(uid, uiKey, newState);
    }
}
