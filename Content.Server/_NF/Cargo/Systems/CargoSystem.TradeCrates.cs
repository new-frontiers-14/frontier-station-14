using System.Threading;
using Content.Server._NF.Trade;
using Content.Server.GameTicking;
using Content.Shared._NF.Trade;
using Content.Shared.Examine;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.Cargo.Systems; // Needs to collide with base namespace

public sealed partial class CargoSystem
{
    [Dependency] private GameTicker _gameTicker = default!;
    private readonly List<EntityUid> _destinations = new();

    private void InitializeTradeCrates()
    {
        SubscribeLocalEvent<TradeCrateComponent, PriceCalculationEvent>(OnTradeCrateGetPriceEvent);
        SubscribeLocalEvent<TradeCrateComponent, ComponentInit>(OnTradeCrateInit);
        SubscribeLocalEvent<TradeCrateComponent, ComponentRemove>(OnTradeCrateRemove);
        SubscribeLocalEvent<TradeCrateComponent, ExaminedEvent>(OnTradeCrateExamined);

        SubscribeLocalEvent<TradeCrateDestinationComponent, ComponentInit>(OnDestinationInit);
        SubscribeLocalEvent<TradeCrateDestinationComponent, ComponentRemove>(OnDestinationRemove);
    }

    private void OnTradeCrateGetPriceEvent(Entity<TradeCrateComponent> ent, ref PriceCalculationEvent ev)
    {
        var owningStation = _station.GetOwningStation(ent);
        var atDestination = ent.Comp.DestinationStation != EntityUid.Invalid
                           && owningStation == ent.Comp.DestinationStation
                           || HasComp<TradeCrateWildcardDestinationComponent>(owningStation);
        ev.Price = atDestination ? ent.Comp.ValueAtDestination : ent.Comp.ValueElsewhere;
        if (ent.Comp.ExpressDeliveryTime != null)
        {
            if (_timing.CurTime <= ent.Comp.ExpressDeliveryTime && atDestination)
                ev.Price += ent.Comp.ExpressOnTimeBonus;
            else if (_timing.CurTime > ent.Comp.ExpressDeliveryTime)
                ev.Price -= ent.Comp.ExpressLatePenalty;
        }
        ev.Price = double.Max(0.0, ev.Price); // Ensure non-negative values.
    }

    private void OnTradeCrateInit(Entity<TradeCrateComponent> ent, ref ComponentInit ev)
    {
        // If there are no available destinations, tough luck.
        if (_destinations.Count > 0)
        {
            var randomIndex = _random.Next(_destinations.Count);
            // Better have more than one destination.
            if (_station.GetOwningStation(ent) == _destinations[randomIndex])
            {
                randomIndex = (randomIndex + 1 + _random.Next(_destinations.Count - 1)) % _destinations.Count;
            }
            var destination = _destinations[randomIndex];
            ent.Comp.DestinationStation = destination;
            if (TryComp<TradeCrateDestinationComponent>(destination, out var destComp))
                _appearance.SetData(ent, TradeCrateVisuals.DestinationIcon, destComp.DestinationProto.Id);
        }

        if (ent.Comp.ExpressDeliveryDuration > TimeSpan.Zero)
        {
            ent.Comp.ExpressDeliveryTime = _timing.CurTime + ent.Comp.ExpressDeliveryDuration;
            _appearance.SetData(ent, TradeCrateVisuals.IsPriority, true);

            ent.Comp.ExpressCancelToken = new CancellationTokenSource();
            Timer.Spawn((int)ent.Comp.ExpressDeliveryDuration.TotalMilliseconds,
                () => DisableTradeCratePriority(ent),
                ent.Comp.ExpressCancelToken.Token);
        }
    }

    private void OnTradeCrateRemove(Entity<TradeCrateComponent> ent, ref ComponentRemove ev)
    {
        ent.Comp.ExpressCancelToken?.Cancel();
    }

    // TODO: move to shared, share delivery time?
    private void OnTradeCrateExamined(Entity<TradeCrateComponent> ent, ref ExaminedEvent ev)
    {
        if (!TryComp(ent.Comp.DestinationStation, out MetaDataComponent? metadata))
            return;

        ev.PushMarkup(Loc.GetString("trade-crate-destination-station", ("destination", metadata.EntityName)));

        if (ent.Comp.ExpressDeliveryTime == null)
            return;

        ev.PushMarkup(ent.Comp.ExpressDeliveryTime >= _timing.CurTime ?
            Loc.GetString("trade-crate-priority-active") :
            Loc.GetString("trade-crate-priority-inactive"));

        var shiftTime = ent.Comp.ExpressDeliveryTime - _gameTicker.RoundStartTimeSpan;
        ev.PushMarkup(Loc.GetString("trade-crate-priority-time", ("time", shiftTime.Value.ToString(@"hh\:mm\:ss"))));
    }

    private void DisableTradeCratePriority(EntityUid uid)
    {
        _appearance.SetData(uid, TradeCrateVisuals.IsPriorityInactive, true);
    }

    private void OnDestinationInit(Entity<TradeCrateDestinationComponent> ent, ref ComponentInit ev)
    {
        if (!_destinations.Contains(ent))
            _destinations.Add(ent);
    }

    private void OnDestinationRemove(Entity<TradeCrateDestinationComponent> ent, ref ComponentRemove ev)
    {
        _destinations.Remove(ent);
    }

    private void CleanupTradeCrateDestinations()
    {
        _destinations.Clear();
    }
}
