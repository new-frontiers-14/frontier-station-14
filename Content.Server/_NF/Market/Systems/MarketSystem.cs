using System.Linq;
using Content.Server._NF.Market.Components;
using Content.Server.Bank;
using Content.Server.Cargo.Systems;
using Content.Shared._NF.Market;
using Content.Shared._NF.Market.BUI;
using Content.Shared.Bank.Components;
using Content.Shared.Stacks;
using Robust.Server.GameObjects;

namespace Content.Server._NF.Market.Systems;

public sealed partial class MarketSystem : SharedMarketSystem
{
    [Dependency] private readonly BankSystem _bank = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private readonly List<MarketData> _marketDataList = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EntitySoldEvent>(OnEntitySoldEvent);
        SubscribeLocalEvent<MarketConsoleComponent, BoundUIOpenedEvent>(OnConsoleUiOpened);

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
                UpdateMarketData(entityPrototypeId, count, ev.Station);
            }
        }
    }

    /// <summary>
    /// Updates the market data list or adds it new if it doesnt exist in there yet.
    /// </summary>
    /// <param name="entityPrototypeId"></param>
    /// <param name="increaseAmount"></param>
    public void UpdateMarketData(string entityPrototypeId, int increaseAmount, EntityUid station)
    {
        var stationNetEntity = GetNetEntity(station);
        // Find the MarketData for the given EntityPrototype
        var marketData = _marketDataList.FirstOrDefault(md => md.Prototype == entityPrototypeId && md.StationUid == stationNetEntity);

        if (marketData != null)
        {
            // If it exists, increase the count
            marketData.Quantity += increaseAmount;
        }
        else
        {
            // If it doesn't exist, create a new MarketData and add it to the list
            _marketDataList.Add(new MarketData(entityPrototypeId, increaseAmount, stationNetEntity));
        }
    }

    private void OnConsoleUiOpened(EntityUid uid, MarketConsoleComponent component, BoundUIOpenedEvent args)
    {
        if (args.Session.AttachedEntity is not { Valid: true } player)
            return;
        if (!TryComp<BankAccountComponent>(player, out var bank))
            return;
        var marketMultiplier = 1.0f;
        if (TryComp<MarketModifierComponent>(uid, out var priceMod))
        {
            marketMultiplier = priceMod.Mod;
        }

        RefreshState(uid, bank.Balance, marketMultiplier, _marketDataList, MarketConsoleUiKey.Default);
    }

    private void RefreshState(
        EntityUid uid,
        int balance,
        float marketMultiplier,
        List<MarketData> data,
        MarketConsoleUiKey uiKey
    )
    {
        var newState = new MarketConsoleInterfaceState(
            balance,
            marketMultiplier,
            data,
            true // TODO add enable/disable functionality
        );

        _ui.TrySetUiState(uid, uiKey, newState);
    }
}
