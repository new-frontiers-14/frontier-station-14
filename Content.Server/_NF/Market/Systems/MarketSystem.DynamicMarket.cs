using Content.Server._NF.CrateMachine;
using Content.Server._NF.GameRule;
using Content.Server._NF.GameTicking.Events;
using Content.Server._NF.Market.Components;
using Content.Server._NF.Market.Extensions;
using Content.Shared._NF.Market;
using Content.Shared._NF.Market.Components;
using Content.Shared._NF.Market.Events;
using Content.Shared._NF.Bank.Components;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Content.Shared._NF.CrateMachine.Components;

namespace Content.Server._NF.Market.Systems;

public sealed partial class MarketSystem
{
    [Dependency] private readonly PointOfInterestSystem _locationSystem = default!;

    private readonly Dictionary<EntityUid, float> DepotBiases = new();

    private void InitializeDynamicMarket()
    {
        SubscribeLocalEvent<StationsGeneratedEvent>(onNewRoundStarted);
    }

    private void onNewRoundStarted(StationsGeneratedEvent _)
    {
        var depotList = EntityQueryEnumerator<DynamicMarketPOIComponent>();
        while (depotList.MoveNext(out var uid, out var _))
        {
            var machineList = GetMarketMachinesOnGrid(uid);
            foreach (var machine in machineList)
            {
                var dynamicComp = EnsureComp<DynamicMarketComponent>(machine.Key);
                var marketMod = machine.Value;
                dynamicComp.CurrentMarketModifier = marketMod.Mod;
                dynamicComp.DefaultMarketModifier = marketMod.Mod;
            }
        }
    }

    private Dictionary<EntityUid, MarketModifierComponent> GetMarketMachinesOnGrid(EntityUid gridUid)
    {
        var machineList = new Dictionary<EntityUid, MarketModifierComponent>();
        var marketMachineList = EntityQueryEnumerator<MarketModifierComponent>();
        while (marketMachineList.MoveNext(out var uid, out var comp))
        {
            if (Transform(uid).ParentUid == gridUid)
            {
                machineList.Add(uid, comp);
            }
        }
        return machineList;
    }
}
