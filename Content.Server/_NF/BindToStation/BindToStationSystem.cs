using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared._NF.BindToStation;
using Content.Shared.Examine;

namespace Content.Server._NF.BindToStation;

public sealed class BindToStationSystem : EntitySystem
{
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly ExtensionCableSystem _extensionCable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BindToStationComponent, ExaminedEvent>(OnBoundItemExamined);
        SubscribeLocalEvent<BindToStationComponent, MapInitEvent>(OnBoundMapInit);
    }

    private void OnBoundItemExamined(EntityUid uid, BindToStationComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || component.BoundStation == null)
            return;

        var stationName = TryComp(component.BoundStation, out MetaDataComponent? meta) ? meta.EntityName : Loc.GetString("bound-to-grid-unknown-station");
        args.PushMarkup(Loc.GetString("bound-to-grid-examine-text", ("shipname", stationName)));
    }

    public void OnBoundMapInit(Entity<BindToStationComponent> ent, ref MapInitEvent args)
    {
        if (TryComp<ExtensionCableReceiverComponent>(ent.Owner, out var receiver)
            && _station.GetOwningStation(ent.Owner) != ent.Comp.BoundStation)
        {
            _extensionCable.Disconnect(ent.Owner, receiver);
        }
    }

    /// <summary>
    /// Binds a given machine to a particular station - the machine will only work when on a grid belonging to that station.
    /// </summary>
    /// <param name="target">The item to be associated with the station.</param>
    /// <param name="station">The station to bind the grid to. If null, unbinds the machine.</param>
    public void BindToStation(EntityUid target, EntityUid? station)
    {
        var binding = EnsureComp<BindToStationComponent>(target);
        binding.BoundStation = station;

        // If this receives power, adjust powered status depending on bound station
        if (TryComp<ExtensionCableReceiverComponent>(target, out var receiver))
        {
            if ((_station.GetOwningStation(target) == station
                || station == null)
                && TryComp(target, out TransformComponent? xform)
                && xform.Anchored)
            {
                _extensionCable.Connect(target, receiver);
            }
            else
            {
                _extensionCable.Disconnect(target, receiver);
            }
        }
    }
}
