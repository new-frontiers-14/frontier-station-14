using System.Linq;
using Content.Server._NF.Cargo.Components;
using Content.Server.Construction;
using Content.Server.Power.Components;
using Content.Shared._NF.Cargo.Components;
using Content.Shared.Cargo;
using Content.Shared.DeviceLinking;
using Content.Shared.Power;
using Robust.Shared.Audio;
using Robust.Shared.Utility;

namespace Content.Server._NF.Cargo.Systems;

public sealed partial class NFCargoSystem
{
    private void InitializeTelepad()
    {
        SubscribeLocalEvent<NFCargoTelepadComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<NFCargoTelepadComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<NFCargoTelepadComponent, UpgradeExamineEvent>(OnUpgradeExamine);
        SubscribeLocalEvent<NFCargoTelepadComponent, PowerChangedEvent>(OnTelepadPowerChange);
        // Shouldn't need re-anchored event
        SubscribeLocalEvent<NFCargoTelepadComponent, AnchorStateChangedEvent>(OnTelepadAnchorChange);
    }
    private void UpdateTelepad(float frameTime)
    {
        var query = EntityQueryEnumerator<NFCargoTelepadComponent>();
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
                !HasComp<NFCargoOrderConsoleComponent>(console))
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

            if (!TryComp<NFStationCargoOrderDatabaseComponent>(station, out var orderDatabase) ||
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
                _audio.PlayPvs(_audio.ResolveSound(comp.TeleportSound), uid, AudioParams.Default.WithVolume(-8f));
                UpdateOrders(station.Value); // Frontier

                comp.CurrentState = CargoTelepadState.Teleporting;
                _appearance.SetData(uid, CargoTelepadVisuals.State, CargoTelepadState.Teleporting, appearance);
            }

            comp.Accumulator += comp.Delay;
        }
    }

    private void OnInit(Entity<NFCargoTelepadComponent> ent, ref ComponentInit args)
    {
        _linker.EnsureSinkPorts(ent, ent.Comp.ReceiverPort);
    }

    private void OnRefreshParts(Entity<NFCargoTelepadComponent> ent, ref RefreshPartsEvent args)
    {
        var rating = args.PartRatings[ent.Comp.MachinePartTeleportDelay] - 1;
        ent.Comp.Delay = ent.Comp.BaseDelay * MathF.Pow(ent.Comp.PartRatingTeleportDelay, rating);
    }

    private void OnUpgradeExamine(Entity<NFCargoTelepadComponent> ent, ref UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade("cargo-telepad-delay-upgrade", ent.Comp.Delay / ent.Comp.BaseDelay);
    }

    private void SetEnabled(Entity<NFCargoTelepadComponent> ent, ApcPowerReceiverComponent? receiver = null,
        TransformComponent? xform = null)
    {
        // False due to AllCompsOneEntity test where they may not have the powerreceiver.
        if (!Resolve(ent, ref receiver, ref xform, false))
            return;

        var disabled = !receiver.Powered || !xform.Anchored;

        // Setting idle state should be handled by Update();
        if (disabled)
            return;

        TryComp<AppearanceComponent>(ent, out var appearance);
        ent.Comp.CurrentState = CargoTelepadState.Unpowered;
        _appearance.SetData(ent, CargoTelepadVisuals.State, CargoTelepadState.Unpowered, appearance);
    }

    private void OnTelepadPowerChange(Entity<NFCargoTelepadComponent> ent, ref PowerChangedEvent args)
    {
        SetEnabled(ent);
    }

    private void OnTelepadAnchorChange(Entity<NFCargoTelepadComponent> ent, ref AnchorStateChangedEvent args)
    {
        SetEnabled(ent);
    }
}
