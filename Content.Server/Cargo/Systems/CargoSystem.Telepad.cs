using System.Linq;
using Content.Server.Cargo.Components;
using Content.Server.Construction;
using Content.Server.Paper;
using Content.Server.Power.Components;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.Power;
using Robust.Shared.Audio;
using Robust.Shared.Utility;
using Robust.Shared.Collections;
using Robust.Shared.Player;
using System.Xml.Schema;
using Content.Server.Station.Components;
using Robust.Shared.Random;

namespace Content.Server.Cargo.Systems;

public sealed partial class CargoSystem
{
    private void InitializeTelepad()
    {
        SubscribeLocalEvent<CargoTelepadComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<CargoTelepadComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<CargoTelepadComponent, UpgradeExamineEvent>(OnUpgradeExamine);
        SubscribeLocalEvent<CargoTelepadComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<CargoTelepadComponent, PowerChangedEvent>(OnTelepadPowerChange);
        // Shouldn't need re-anchored event
        SubscribeLocalEvent<CargoTelepadComponent, AnchorStateChangedEvent>(OnTelepadAnchorChange);
    }
    private void UpdateTelepad(float frameTime)
    {
        var query = EntityQueryEnumerator<CargoTelepadComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            // Don't EntityQuery for it as it's not required.
            TryComp<AppearanceComponent>(uid, out var appearance);

            if (comp.CurrentState == CargoTelepadState.Unpowered)
            {
                comp.CurrentState = CargoTelepadState.Idle;
                _appearance.SetData(uid, CargoTelepadVisuals.State, CargoTelepadState.Idle, appearance);
                comp.Accumulator = comp.Delay;
                continue;
            }

            if (!TryComp<DeviceLinkSinkComponent>(uid, out var sinkComponent) ||
                sinkComponent.LinkedSources.FirstOrNull() is not { } console ||
                !HasComp<CargoOrderConsoleComponent>(console))
            {
                comp.Accumulator = comp.Delay;
                continue;
            }

            comp.Accumulator -= frameTime;

            // Uhh listen teleporting takes time and I just want the 1 float.
            if (comp.Accumulator > 0f)
            {
                comp.CurrentState = CargoTelepadState.Idle;
                _appearance.SetData(uid, CargoTelepadVisuals.State, CargoTelepadState.Idle, appearance);
                continue;
            }

            var station = _station.GetOwningStation(console);

            if (!TryComp<StationCargoOrderDatabaseComponent>(station, out var orderDatabase) ||
                orderDatabase.Orders.Count == 0)
            {
                comp.Accumulator += comp.Delay;
                continue;
            }

            // Frontier - This makes sure telepads spawn goods of linked computers only. //TODO: FIx This Again
             List<NetEntity> consoleUidList = sinkComponent.LinkedSources.Select(item => EntityManager.GetNetEntity(item)).ToList();

            var xform = Transform(uid);
            if (FulfillNextOrder(consoleUidList, orderDatabase, xform.Coordinates, comp.PrinterOutput))
            {
                _audio.PlayPvs(_audio.GetSound(comp.TeleportSound), uid, AudioParams.Default.WithVolume(-8f));
                UpdateOrders(station.Value, orderDatabase);

                comp.CurrentState = CargoTelepadState.Teleporting;
                _appearance.SetData(uid, CargoTelepadVisuals.State, CargoTelepadState.Teleporting, appearance);
            }

            comp.Accumulator += comp.Delay;
        }
    }

    private void OnInit(EntityUid uid, CargoTelepadComponent telepad, ComponentInit args)
    {
        _linker.EnsureSinkPorts(uid, telepad.ReceiverPort);
    }

    private void OnRefreshParts(EntityUid uid, CargoTelepadComponent component, RefreshPartsEvent args)
    {
        var rating = args.PartRatings[component.MachinePartTeleportDelay] - 1;
        component.Delay = component.BaseDelay * MathF.Pow(component.PartRatingTeleportDelay, rating);
    }

    private void OnUpgradeExamine(EntityUid uid, CargoTelepadComponent component, UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade("cargo-telepad-delay-upgrade", component.Delay / component.BaseDelay);
    }

    private void OnShutdown(Entity<CargoTelepadComponent> ent, ref ComponentShutdown args)
    {
        //if (ent.Comp.CurrentOrders.Count == 0) //Frontier - todo: find a smarter way to maybe fix this otherwise its exploity by forcing crate spawn on rando station
            return;

        if (_station.GetStations().Count == 0)
            return;

        if (_station.GetOwningStation(ent) is not { } station)
        {
            station = _random.Pick(_station.GetStations().Where(HasComp<StationCargoOrderDatabaseComponent>).ToList());
        }

        if (!TryComp<StationCargoOrderDatabaseComponent>(station, out var db) ||
            !TryComp<StationDataComponent>(station, out var data))
            return;

        //foreach (var order in ent.Comp.CurrentOrders)
        //{
            //TryFulfillOrder((station, data), order, db); //Frontier TODO: Fix this?
        //}
    }

    private void SetEnabled(EntityUid uid, CargoTelepadComponent component, ApcPowerReceiverComponent? receiver = null,
        TransformComponent? xform = null)
    {
        // False due to AllCompsOneEntity test where they may not have the powerreceiver.
        if (!Resolve(uid, ref receiver, ref xform, false))
            return;

        var disabled = !receiver.Powered || !xform.Anchored;

        // Setting idle state should be handled by Update();
        if (disabled)
            return;

        TryComp<AppearanceComponent>(uid, out var appearance);
        component.CurrentState = CargoTelepadState.Unpowered;
        _appearance.SetData(uid, CargoTelepadVisuals.State, CargoTelepadState.Unpowered, appearance);
    }

    private void OnTelepadPowerChange(EntityUid uid, CargoTelepadComponent component, ref PowerChangedEvent args)
    {
        SetEnabled(uid, component);
    }

    private void OnTelepadAnchorChange(EntityUid uid, CargoTelepadComponent component, ref AnchorStateChangedEvent args)
    {
        SetEnabled(uid, component);
    }
}
