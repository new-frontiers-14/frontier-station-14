using System.Threading;
using Content.Server._NF.Trade;
using Content.Shared._NF.Trade;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Robust.Shared.GameObjects;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.Cargo.Systems; // Needs to collide with base namespace

public sealed partial class CargoSystem
{
    private List<EntityUid> _destinations = new();

    private void InitializeTradeCrates()
    {
        SubscribeLocalEvent<TradeCrateComponent, PriceCalculationEvent>(OnTradeCrateGetPriceEvent);
        SubscribeLocalEvent<TradeCrateComponent, ComponentInit>(OnTradeCrateInit);
        SubscribeLocalEvent<TradeCrateComponent, ComponentRemove>(OnTradeCrateRemove);
        SubscribeLocalEvent<TradeCrateComponent, ExaminedEvent>(OnTradeCrateExamined);

        SubscribeLocalEvent<TradeCrateDestinationComponent, ComponentInit>(OnDestinationInit);
        SubscribeLocalEvent<TradeCrateDestinationComponent, ComponentRemove>(OnDestinationRemove);
    }

    private void OnTradeCrateGetPriceEvent(EntityUid uid, TradeCrateComponent component, ref PriceCalculationEvent ev)
    {
        bool isDestinated = component.DestinationStation != EntityUid.Invalid && _station.GetOwningStation(uid) == component.DestinationStation;
        ev.Price = isDestinated ? component.ValueAtDestination : component.ValueElsewhere;
        if (component.ExpressDeliveryTime != null)
        {
            if (_timing.CurTime <= component.ExpressDeliveryTime && isDestinated)
                ev.Price += component.ExpressOnTimeBonus;
            else if (_timing.CurTime > component.ExpressDeliveryTime)
                ev.Price -= component.ExpressLatePenalty;
        }
        ev.Price = double.Max(0.0, ev.Price); // Ensure non-negative values.
    }

    private void OnTradeCrateInit(EntityUid uid, TradeCrateComponent component, ref ComponentInit ev)
    {
        // If there are no available destinations, tough luck.
        if (_destinations.Count > 0)
        {
            var destination = _destinations[_random.Next(_destinations.Count)];
            component.DestinationStation = destination;
            if (TryComp<TradeCrateDestinationComponent>(destination, out var destComp))
                _appearance.SetData(uid, TradeCrateVisuals.DestinationIcon, destComp.DestinationProto);
        }

        if (component.ExpressDeliveryDuration > TimeSpan.Zero)
        {
            component.ExpressDeliveryTime = _timing.CurTime + component.ExpressDeliveryDuration;
            _appearance.SetData(uid, TradeCrateVisuals.IsPriority, true);

            component.PriorityCancelToken = new CancellationTokenSource();
            Timer.Spawn((int)component.ExpressDeliveryDuration.TotalMilliseconds,
                () => DisableTradeCratePriority(uid, component),
                component.PriorityCancelToken.Token);
        }
    }

    private void OnTradeCrateRemove(EntityUid uid, TradeCrateComponent component, ref ComponentRemove ev)
    {
        component.PriorityCancelToken?.Cancel();
    }

    private void OnTradeCrateExamined(EntityUid uid, TradeCrateComponent component, ref ExaminedEvent ev)
    {
        if (!TryComp(component.DestinationStation, out MetaDataComponent? metadata))
            return;

        ev.PushMarkup(Loc.GetString("trade-crate-destination-station", ("destination", metadata.EntityName)));
        if (component.ExpressDeliveryTime != null)
        {
            if (component.ExpressDeliveryTime >= _timing.CurTime)
                ev.PushMarkup(Loc.GetString("trade-crate-priority-active"));
            else
                ev.PushMarkup(Loc.GetString("trade-crate-priority-inactive"));
        }
    }

    private void DisableTradeCratePriority(EntityUid uid, TradeCrateComponent component)
    {
        _appearance.SetData(uid, TradeCrateVisuals.IsPriorityInactive, true);
    }

    private void OnDestinationInit(EntityUid uid, TradeCrateDestinationComponent component, ref ComponentInit ev)
    {
        if (!_destinations.Contains(uid))
            _destinations.Add(uid);
    }

    private void OnDestinationRemove(EntityUid uid, TradeCrateDestinationComponent component, ref ComponentRemove ev)
    {
        _destinations.Remove(uid);
    }

    private void CleanupTradeCrateDestinations(RoundRestartCleanupEvent ev)
    {
        _destinations.Clear();
    }
}
