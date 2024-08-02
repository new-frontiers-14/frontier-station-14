using System.Linq;
using System.Threading.Tasks;
using Content.Server._NF.Market.Components;
using Content.Server._NF.Market.Extensions;
using Content.Shared._NF.Market;
using Content.Shared._NF.Market.Components;
using Content.Shared._NF.Market.Events;
using Content.Shared.Bank.Components;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using static Content.Shared._NF.Market.Components.SharedCrateMachineComponent;

namespace Content.Server._NF.Market.Systems;

public sealed partial class MarketSystem
{
    private void InitializeCrateMachine()
    {
        SubscribeLocalEvent<MarketConsoleComponent, CrateMachinePurchaseMessage>(OnMarketConsolePurchaseCrateMessage);
    }

    /// <summary>
    /// Calculates distance between two EntityCoordinates on the same grid.
    /// Used to check for cargo pallets around the console instead of on the grid.
    /// </summary>
    /// <param name="point1">the first point</param>
    /// <param name="point2">the second point</param>
    /// <returns></returns>
    private static double CalculateDistance(EntityCoordinates point1, EntityCoordinates point2)
    {
        var xDifference = point2.X - point1.X;
        var yDifference = point2.Y - point1.Y;

        return Math.Sqrt(xDifference * xDifference + yDifference * yDifference);
    }

    private void OnMarketConsolePurchaseCrateMessage(EntityUid consoleUid,
        MarketConsoleComponent component,
        ref CrateMachinePurchaseMessage args)
    {
        var crateMachineQuery = AllEntityQuery<CrateMachineComponent, TransformComponent>();

        var marketMod = 1f;
        if (TryComp<MarketModifierComponent>(consoleUid, out var marketModComponent))
        {
            marketMod = marketModComponent.Mod;
        }

        // Stop here if we dont have a grid.
        if (Transform(consoleUid).GridUid == null)
            return;

        var consoleGridUid = Transform(consoleUid).GridUid!.Value;
        while (crateMachineQuery.MoveNext(out var crateMachineUid, out var comp, out var compXform))
        {
            // Skip crate machines that aren't mounted on a grid.
            if (Transform(crateMachineUid).GridUid == null)
                continue;
            // Skip crate machines that are not on the same grid.
            if (Transform(crateMachineUid).GridUid!.Value != consoleGridUid)
                continue;
            var distance = CalculateDistance(compXform.Coordinates, Transform(consoleUid).Coordinates);
            var maxCrateMachineDistance = component.MaxCrateMachineDistance;

            // Get the mapped checking distance from the console
            if (TryComp<MarketConsoleComponent>(consoleUid, out var cargoShuttleComponent))
            {
                maxCrateMachineDistance = cargoShuttleComponent.MaxCrateMachineDistance;
            }

            var isTooFarAway = distance > maxCrateMachineDistance;

            if (!compXform.Anchored || isTooFarAway)
            {
                continue;
            }

            // We found the first nearby compatible crate machine.
            OnPurchaseCrateMessage(crateMachineUid, comp, component, marketMod, args);

            break;
        }
    }

    private void OnPurchaseCrateMessage(EntityUid crateMachineUid,
        SharedCrateMachineComponent component,
        MarketConsoleComponent consoleComponent,
        float marketMod,
        CrateMachinePurchaseMessage args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        if (!TryComp<BankAccountComponent>(player, out var bankAccount))
            return;

        TrySpawnCrate(crateMachineUid, player, component, consoleComponent, marketMod, bankAccount);
    }

    /// <summary>
    /// Checks if there is a crate on the crate machine
    /// </summary>
    /// <param name="crateMachineUid">The Uid of the crate machine</param>
    /// <param name="cratePrototype">The prototype of the crate being spawned by the crate machine</param>
    /// <returns>False if not occupied, true if it is.</returns>
    private bool IsCrateMachineOccupied(EntityUid crateMachineUid, string cratePrototype)
    {
        if (!TryComp<TransformComponent>(crateMachineUid, out var crateMachineTransform))
            return true;
        var tileRef = crateMachineTransform.Coordinates.GetTileRef(EntityManager, _mapManager);
        if (tileRef == null)
        {
            return true;
        }

        // Finally check if there is a crate intersecting the crate machine.
        return _lookup.GetLocalEntitiesIntersecting(tileRef.Value, flags: LookupFlags.All | LookupFlags.Approximate)
            .Any(entity => _entityManager.GetComponent<MetaDataComponent>(entity).EntityPrototype?.ID ==
                           cratePrototype);
    }

    private bool _isAnimationRunning = false;

    private void TrySpawnCrate(EntityUid crateMachineUid,
        EntityUid player,
        SharedCrateMachineComponent component,
        MarketConsoleComponent consoleComponent,
        float marketMod,
        BankAccountComponent playerBank)
    {
        if (_isAnimationRunning || IsCrateMachineOccupied(crateMachineUid, component.CratePrototype))
            return;

        var cartBalance = MarketDataExtensions.GetMarketValue(consoleComponent.CartDataList, marketMod);
        if (playerBank.Balance < cartBalance)
            return;

        var spawnList = new List<MarketData>();
        for (var i = 0; i < 30 && consoleComponent.CartDataList.Count > 0; i++)
        {
            var marketData = consoleComponent.CartDataList.First();

            spawnList.Add(marketData);

            if (marketData.Quantity > 1)
            {
                consoleComponent.CartDataList.First().Quantity -= 1;
            }
            else
            {
                consoleComponent.CartDataList.Remove(marketData);
            }
        }

        // Withdraw spesos from player
        var spawnCost = int.Abs(MarketDataExtensions.GetMarketValue(spawnList, marketMod));
        if (!_bankSystem.TryBankWithdraw(player, spawnCost))
            return;

        var xform = Transform(crateMachineUid);

        _isAnimationRunning = true;

        Task.Run(async () =>
        {
            UpdateVisualState(crateMachineUid, component);
            Dirty(crateMachineUid, component);
            await Task.Delay(3000);
            var targetCrate = Spawn(component.CratePrototype, xform.Coordinates);
            UpdateVisualState(crateMachineUid, component, false);
            Dirty(crateMachineUid, component);
            _isAnimationRunning = false;
            SpawnCrateItems(spawnList, targetCrate);
        });
    }

    private void SpawnCrateItems(List<MarketData> spawnList, EntityUid targetCrate)
    {
        foreach (var data in spawnList)
        {
            var spawn = Spawn(data.Prototype, Transform(targetCrate).Coordinates);
            _storage.Insert(spawn, targetCrate);
        }
    }

    private void UpdateVisualState(EntityUid uid, SharedCrateMachineComponent component, bool isOpening = true)
    {
        if (!TryComp(uid, out AppearanceComponent? appearance))
        {
            return;
        }

        // Unanchored should not animate.
        if (!Transform(uid).Anchored)
        {
            return;
        }

        if (isOpening)
        {
            _appearanceSystem.SetData(uid,
                CrateMachineVisuals.VisualState,
                CrateMachineVisualState.Opening,
                appearance);
        }
        else
        {
            _appearanceSystem.SetData(uid,
                CrateMachineVisuals.VisualState,
                CrateMachineVisualState.Closing,
                appearance);
        }
    }
}
