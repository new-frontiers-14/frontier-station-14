namespace Content.Server._NF.Cargo.Systems;

using Components;
using Construction;
using Content.Server.Power.Components;
using Content.Shared._NF.Cargo;
using Content.Shared._NF.Cargo.Components;
using Content.Shared.Cargo.Components;
using Content.Shared.DeviceLinking;
using Robust.Shared.Audio;
using Robust.Shared.Utility;

public sealed partial class FrontierCargoSystem
{
    private void InitializeTelepad()
    {
        SubscribeLocalEvent<FrontierCargoTelepadComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<FrontierCargoTelepadComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<FrontierCargoTelepadComponent, UpgradeExamineEvent>(OnUpgradeExamine);
        SubscribeLocalEvent<FrontierCargoTelepadComponent, PowerChangedEvent>(OnTelepadPowerChange);
        // Shouldn't need re-anchored event
        SubscribeLocalEvent<FrontierCargoTelepadComponent, AnchorStateChangedEvent>(OnTelepadAnchorChange);
    }

    private void UpdateTelepad(float frameTime)
    {
        var query = EntityQueryEnumerator<FrontierCargoTelepadComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            // Don't EntityQuery for it as it's not required.
            TryComp<AppearanceComponent>(uid, out var appearance);

            if (comp.CurrentState == FrontierCargoTelepadState.Unpowered)
            {
                comp.CurrentState = FrontierCargoTelepadState.Idle;
                _appearance.SetData(uid, FrontierCargoTelepadVisuals.State, FrontierCargoTelepadState.Idle, appearance);
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
                comp.CurrentState = FrontierCargoTelepadState.Idle;
                _appearance.SetData(uid, FrontierCargoTelepadVisuals.State, FrontierCargoTelepadState.Idle, appearance);
                continue;
            }

            var station = _station.GetOwningStation(console);

            if (!TryComp<FrontierStationCargoOrderDatabaseComponent>(station, out var orderDatabase) ||
                orderDatabase.Orders.Count == 0)
            {
                comp.Accumulator += comp.Delay;
                continue;
            }

            var xform = Transform(uid);
            if (FulfillNextOrder(orderDatabase, xform.Coordinates, comp.PrinterOutput))
            {
                _audio.PlayPvs(_audio.GetSound(comp.TeleportSound), uid, AudioParams.Default.WithVolume(-8f));
                UpdateOrders(station.Value, orderDatabase);

                comp.CurrentState = FrontierCargoTelepadState.Teleporting;
                _appearance.SetData(uid, FrontierCargoTelepadVisuals.State, FrontierCargoTelepadState.Teleporting, appearance);
            }

            comp.Accumulator += comp.Delay;
        }
    }

    private void OnInit(EntityUid uid, FrontierCargoTelepadComponent telepad, ComponentInit args)
    {
        _linker.EnsureSinkPorts(uid, telepad.ReceiverPort);
    }

    private void OnRefreshParts(EntityUid uid, FrontierCargoTelepadComponent component, RefreshPartsEvent args)
    {
        var rating = args.PartRatings[component.MachinePartTeleportDelay] - 1;
        component.Delay = component.BaseDelay * MathF.Pow(component.PartRatingTeleportDelay, rating);
    }

    private void OnUpgradeExamine(EntityUid uid, FrontierCargoTelepadComponent component, UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade("cargo-telepad-delay-upgrade", component.Delay / component.BaseDelay);
    }

    private void SetEnabled(EntityUid uid, FrontierCargoTelepadComponent component, ApcPowerReceiverComponent? receiver = null,
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
        component.CurrentState = FrontierCargoTelepadState.Unpowered;
        _appearance.SetData(uid, FrontierCargoTelepadVisuals.State, FrontierCargoTelepadState.Unpowered, appearance);
    }

    private void OnTelepadPowerChange(EntityUid uid, FrontierCargoTelepadComponent component, ref PowerChangedEvent args)
    {
        SetEnabled(uid, component);
    }

    private void OnTelepadAnchorChange(EntityUid uid, FrontierCargoTelepadComponent component, ref AnchorStateChangedEvent args)
    {
        SetEnabled(uid, component);
    }
}
