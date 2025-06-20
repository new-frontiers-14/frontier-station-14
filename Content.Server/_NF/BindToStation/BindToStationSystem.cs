using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared._NF.BindToStation;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;

namespace Content.Server._NF.BindToStation;

public sealed class BindToStationSystem : EntitySystem
{
    [Dependency] private readonly ExtensionCableSystem _extensionCable = default!;
    [Dependency] private readonly StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationBoundObjectComponent, ExaminedEvent>(OnBoundItemExamined);
        SubscribeLocalEvent<StationBoundObjectComponent, MapInitEvent>(OnBoundMapInit);
        SubscribeLocalEvent<StationBoundObjectComponent, GotEmaggedEvent>(OnBoundEmagged);
        SubscribeLocalEvent<StationBoundObjectComponent, GotUnEmaggedEvent>(OnBoundUnemagged);
    }

    private void OnBoundItemExamined(EntityUid uid, StationBoundObjectComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || component.BoundStation == null || !component.Enabled)
            return;

        var stationName = TryComp(component.BoundStation, out MetaDataComponent? meta) ? meta.EntityName : Loc.GetString("bound-to-grid-unknown-station");
        args.PushMarkup(Loc.GetString("bound-to-grid-examine-text", ("shipname", stationName)));
    }

    // Ensure consistency for station-bound machines
    public void OnBoundMapInit(Entity<StationBoundObjectComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Enabled
            && TryComp<ExtensionCableReceiverComponent>(ent.Owner, out var receiver)
            && _station.GetOwningStation(ent.Owner) != ent.Comp.BoundStation)
        {
            _extensionCable.Disconnect((ent.Owner, receiver));
        }
    }

    public void OnBoundEmagged(Entity<StationBoundObjectComponent> ent, ref GotEmaggedEvent args)
    {
        // Don't check handled - machines may be emagged separately by other types.
        if (!args.Type.HasFlag(EmagType.StationBound))
            return;

        if (TryComp<EmaggedComponent>(ent, out var emagged) && emagged.EmagType.HasFlag(EmagType.StationBound))
            return;

        // Already disabled or not bound.
        if (!ent.Comp.Enabled || ent.Comp.BoundStation == null)
            return;

        // Disable the machine binding, leave the repeatable field as-is in case other machines set it.
        BindToStation(ent, ent.Comp.BoundStation, false);
        args.Handled = true;
    }

    public void OnBoundUnemagged(Entity<StationBoundObjectComponent> ent, ref GotUnEmaggedEvent args)
    {
        // Don't check handled - machines may be emagged separately by other types.
        if (!args.Type.HasFlag(EmagType.StationBound))
            return;

        if (!TryComp<EmaggedComponent>(ent, out var emagged) || !emagged.EmagType.HasFlag(EmagType.StationBound))
            return;

        // Already enabled or not bound (enabling does nothing).
        if (ent.Comp.Enabled || ent.Comp.BoundStation == null)
            return;

        // Re-enable the machine binding, leave the repeatable field as-is in case other machines set it.
        BindToStation(ent, ent.Comp.BoundStation, true);
        args.Handled = true;
    }

    /// <summary>
    /// Binds a given machine to a particular station - the machine will only work when on a grid belonging to that station.
    /// </summary>
    /// <param name="target">The item to be associated with the station.</param>
    /// <param name="station">The station to bind the grid to. If null, unbinds the machine.</param>
    public void BindToStation(EntityUid target, EntityUid? station, bool enabled = true)
    {
        var binding = EnsureComp<StationBoundObjectComponent>(target);
        binding.BoundStation = station;
        binding.Enabled = enabled;

        // If this receives power, adjust powered status depending on bound station
        if (TryComp<ExtensionCableReceiverComponent>(target, out var receiver))
        {
            if ((!enabled
                || _station.GetOwningStation(target) == station
                || station == null)
                && TryComp(target, out TransformComponent? xform)
                && xform.Anchored)
            {
                _extensionCable.Connect((target, receiver));
            }
            else
            {
                _extensionCable.Disconnect((target, receiver));
            }
        }
    }
}
