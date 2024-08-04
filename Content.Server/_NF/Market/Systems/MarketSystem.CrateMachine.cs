using System.Linq;
using Content.Server._NF.Market.Components;
using Content.Server._NF.Market.Extensions;
using Content.Server.Power.Components;
using Content.Shared._NF.Market;
using Content.Shared._NF.Market.Components;
using Content.Shared._NF.Market.Events;
using Content.Shared.Bank.Components;
using Content.Shared.Maps;
using Content.Shared.Popups;
using Content.Shared.Prototypes;
using Content.Shared.Stacks;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using static Content.Shared._NF.Market.Components.CrateMachineComponent;

namespace Content.Server._NF.Market.Systems;

public sealed partial class MarketSystem
{
    private void InitializeCrateMachine()
    {
        SubscribeLocalEvent<MarketConsoleComponent, CrateMachinePurchaseMessage>(OnMarketConsolePurchaseCrateMessage);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CrateMachineComponent, ApcPowerReceiverComponent>();
        while (query.MoveNext(out var uid, out var crateMachine, out var receiver))
        {
            if (!receiver.Powered)
                continue;

            ProcessOpeningAnimation(uid, frameTime, crateMachine);
            ProcessClosingAnimation(uid, frameTime, crateMachine);
        }
    }

    private void ProcessOpeningAnimation(EntityUid uid, float frameTime, CrateMachineComponent comp)
    {
        if (comp.OpeningTimeRemaining <= 0)
            return;

        comp.OpeningTimeRemaining -= frameTime;

        // Automatically start closing after it finishes open animation.
        // Also spawns the crate.
        if (comp.OpeningTimeRemaining <= 0)
        {
            var targetCrate = Spawn(comp.CratePrototype, Transform(uid).Coordinates);
            SpawnCrateItems(comp.ItemsToSpawn, targetCrate);
            comp.DidTakeCrate = false;
            comp.ItemsToSpawn = [];
        }

        // Update at the end so the closing animation can start automatically.
        UpdateVisualState(uid, comp);
    }

    private void ProcessClosingAnimation(EntityUid uid, float frameTime, CrateMachineComponent comp)
    {
        if (!comp.DidTakeCrate && !IsCrateMachineOccupied(uid, comp, true))
        {
            comp.DidTakeCrate = true;
            comp.ClosingTimeRemaining = comp.ClosingTime;
        }
        if (comp.ClosingTimeRemaining <= 0)
            return;

        comp.ClosingTimeRemaining -= frameTime;
        UpdateVisualState(uid, comp);
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
            var isBusy = IsCrateMachineOccupied(crateMachineUid, comp);

            if (!compXform.Anchored || isTooFarAway || isBusy)
            {
                continue;
            }

            // We found the first nearby compatible crate machine.
            OnPurchaseCrateMessage(crateMachineUid, consoleUid, comp, component, marketMod, args);

            return;
        }
        _popup.PopupEntity(Loc.GetString("market-no-crate-machine-available"), consoleUid, Filter.PvsExcept(consoleUid), true);
        _audio.PlayPredicted(component.ErrorSound, consoleUid, null, AudioParams.Default.WithMaxDistance(5f));
    }

    private void OnPurchaseCrateMessage(EntityUid crateMachineUid,
        EntityUid consoleUid,
        CrateMachineComponent component,
        MarketConsoleComponent consoleComponent,
        float marketMod,
        CrateMachinePurchaseMessage args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        if (!TryComp<BankAccountComponent>(player, out var bankAccount))
            return;

        TrySpawnCrate(crateMachineUid, player, consoleUid, component, consoleComponent, marketMod, bankAccount);
    }

    /// <summary>
    /// Checks if there is a crate on the crate machine
    /// </summary>
    /// <param name="crateMachineUid">The Uid of the crate machine</param>
    /// <param name="component">The crate machine component</param>
    /// <param name="ignoreAnimation">Ignores animation checks</param>
    /// <returns>False if not occupied, true if it is.</returns>
    private bool IsCrateMachineOccupied(EntityUid crateMachineUid, CrateMachineComponent component, bool ignoreAnimation = false)
    {
        if (!TryComp<TransformComponent>(crateMachineUid, out var crateMachineTransform))
            return true;
        var tileRef = crateMachineTransform.Coordinates.GetTileRef(EntityManager, _mapManager);
        if (tileRef == null)
            return true;

        if (!ignoreAnimation && (component.OpeningTimeRemaining > 0 || component.ClosingTimeRemaining > 0f))
            return true;

        // Finally check if there is a crate intersecting the crate machine.
        return _lookup.GetLocalEntitiesIntersecting(tileRef.Value, flags: LookupFlags.All | LookupFlags.Approximate)
            .Any(entity => _entityManager.GetComponent<MetaDataComponent>(entity).EntityPrototype?.ID ==
                           component.CratePrototype);
    }

    private void TrySpawnCrate(EntityUid crateMachineUid,
        EntityUid player,
        EntityUid consoleUid,
        CrateMachineComponent component,
        MarketConsoleComponent consoleComponent,
        float marketMod,
        BankAccountComponent playerBank)
    {
        var cartBalance = MarketDataExtensions.GetMarketValue(consoleComponent.CartDataList, marketMod);
        if (playerBank.Balance < cartBalance)
            return;

        // Withdraw spesos from player
        var spawnCost = int.Abs(MarketDataExtensions.GetMarketValue(consoleComponent.CartDataList, marketMod));
        if (!_bankSystem.TryBankWithdraw(player, spawnCost))
        {
            _popup.PopupEntity(Loc.GetString("market-insufficient-funds"), consoleUid, player);
            _audio.PlayPredicted(consoleComponent.ErrorSound, consoleUid, null, AudioParams.Default.WithMaxDistance(5f));
            return;
        }
        _audio.PlayPredicted(consoleComponent.SuccessSound, consoleUid, null, AudioParams.Default.WithMaxDistance(5f));

        component.OpeningTimeRemaining = component.OpeningTime;
        component.ItemsToSpawn = consoleComponent.CartDataList;
        consoleComponent.CartDataList = [];
        UpdateVisualState(crateMachineUid, component);
    }

    private void SpawnCrateItems(List<MarketData> spawnList, EntityUid targetCrate)
    {
        var coordinates = Transform(targetCrate).Coordinates;
        foreach (var data in spawnList)
        {
            if (data.StackPrototype != null && _prototypeManager.TryIndex(data.StackPrototype, out var stackPrototype))
            {
                var entityList = _stackSystem.SpawnMultiple(stackPrototype.Spawn, data.Quantity, coordinates);
                foreach (var entity in entityList)
                {
                    _storage.Insert(entity, targetCrate);
                }
            }
            else
            {
                var spawn = Spawn(data.Prototype, coordinates);
                _storage.Insert(spawn, targetCrate);
            }
        }
    }

    private void UpdateVisualState(EntityUid uid, CrateMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.OpeningTimeRemaining > 0)
            _appearanceSystem.SetData(uid, CrateMachineVisuals.VisualState, CrateMachineVisualState.Opening);
        else if (component.ClosingTimeRemaining > 0)
            _appearanceSystem.SetData(uid, CrateMachineVisuals.VisualState, CrateMachineVisualState.Closing);
        else if (!component.DidTakeCrate)
            _appearanceSystem.SetData(uid, CrateMachineVisuals.VisualState, CrateMachineVisualState.Open);
        else
            _appearanceSystem.SetData(uid, CrateMachineVisuals.VisualState, CrateMachineVisualState.Closed);
    }
}
