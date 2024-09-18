using Content.Server.Power.Components;
using Content.Server.Emp;
using Content.Server.PowerCell;
using Content.Shared.Examine;
using Content.Shared.Power;
using Content.Shared.PowerCell.Components;
using Content.Shared.Emp;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Storage.Components;
using Robust.Server.Containers;
using Content.Shared.Whitelist;

namespace Content.Server.Power.EntitySystems;

[UsedImplicitly]
internal sealed class ChargerSystem : EntitySystem
{
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ChargerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ChargerComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<ChargerComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<ChargerComponent, EntRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<ChargerComponent, ContainerIsInsertingAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<ChargerComponent, InsertIntoEntityStorageAttemptEvent>(OnEntityStorageInsertAttempt);
        SubscribeLocalEvent<ChargerComponent, ExaminedEvent>(OnChargerExamine);

        SubscribeLocalEvent<ChargerComponent, ChargerUpdateStatusEvent>(OnUpdateStatus); // Frontier: Upstream - #28984

        SubscribeLocalEvent<ChargerComponent, EmpPulseEvent>(OnEmpPulse);
        SubscribeLocalEvent<ChargerComponent, EmpDisabledRemoved>(OnEmpDisabledRemoved); // Frontier: Upstream - #28984
    }

    private void OnStartup(EntityUid uid, ChargerComponent component, ComponentStartup args)
    {
        UpdateStatus(uid, component);
    }

    private void OnChargerExamine(EntityUid uid, ChargerComponent component, ExaminedEvent args)
    {
        using (args.PushGroup(nameof(ChargerComponent)))
        {
            // rate at which the charger charges
            args.PushMarkup(Loc.GetString("charger-examine", ("color", "yellow"), ("chargeRate", (int) component.ChargeRate)));

            // try to get contents of the charger
            if (!_container.TryGetContainer(uid, component.SlotId, out var container))
                return;

            if (HasComp<PowerCellSlotComponent>(uid))
                return;

            // if charger is empty and not a power cell type charger, add empty message
            // power cells have their own empty message by default, for things like flash lights
            if (container.ContainedEntities.Count == 0)
            {
                args.PushMarkup(Loc.GetString("charger-empty"));
            }
            else
            {
                // add how much each item is charged it 
                foreach (var contained in container.ContainedEntities)
                {
                    if (!TryComp<BatteryComponent>(contained, out var battery))
                        continue;

                    var chargePercentage = (battery.CurrentCharge / battery.MaxCharge) * 100;
                    args.PushMarkup(Loc.GetString("charger-content", ("chargePercentage", (int) chargePercentage)));
                }
            }
        }
    }

    private void StartChargingBattery(EntityUid uid, ChargerComponent component, EntityUid target) // Frontier: Upstream - #28984
    {
        bool charge = true;

        if (HasComp<EmpDisabledComponent>(uid))
            charge = false;
        else
        if (!TryComp<BatteryComponent>(target, out var battery))
            charge = false;
        else
        if (Math.Abs(battery.MaxCharge - battery.CurrentCharge) < 0.01)
            charge = false;

        // wrap functionality in an if statement instead of returning...
        if (charge)
        {
            var charging = EnsureComp<ChargingComponent>(target);
            charging.ChargerUid = uid;
            charging.ChargerComponent = component;
        }

        // ...so the status always updates (for insertin a power cell)
        UpdateStatus(uid, component);
    }

    private void StopChargingBattery(EntityUid uid, ChargerComponent component, EntityUid target) // Frontier: Upstream - #28984
    {
        RemComp<ChargingComponent>(target);
        UpdateStatus(uid, component);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ChargingComponent>(); // Frontier: Upstream - #28984
        while (query.MoveNext(out var uid, out var charging)) // Frontier: Upstream - #28984
        {
            if (!HasComp<ChargerComponent>(charging.ChargerUid)) // Frontier: Upstream - #28984
                continue;

            if (charging.ChargerComponent.Status == CellChargerStatus.Off || charging.ChargerComponent.Status == CellChargerStatus.Empty) // Frontier: Upstream - #28984
                continue;

            // Frontier: Upstream - #28984 Start
            //foreach (var contained in container.ContainedEntities)
            //{
            //    TransferPower(uid, contained, charger, frameTime);
            //}

            if (HasComp<EmpDisabledComponent>(charging.ChargerUid))
                continue;

            if (!TryComp<BatteryComponent>(uid, out var battery))
                continue;

            if (Math.Abs(battery.MaxCharge - battery.CurrentCharge) < 0.01)
                StopChargingBattery(charging.ChargerUid, charging.ChargerComponent, uid);
            TransferPower(charging.ChargerUid, uid, charging.ChargerComponent, frameTime);
            // Frontier: Upstream - #28984 End
        }
    }

    private void OnPowerChanged(EntityUid uid, ChargerComponent component, ref PowerChangedEvent args)
    {
        UpdateStatus(uid, component);
    }

    private void OnInserted(EntityUid uid, ChargerComponent component, EntInsertedIntoContainerMessage args)
    {
        if (!component.Initialized)
            return;

        if (args.Container.ID != component.SlotId)
            return;

        StartChargingBattery(uid, component, args.Entity); // Frontier: Upstream - #28984
    }

    private void OnRemoved(EntityUid uid, ChargerComponent component, EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != component.SlotId)
            return;

        StopChargingBattery(uid, component, args.Entity); // Frontier: Upstream - #28984
    }

    /// <summary>
    ///     Verify that the entity being inserted is actually rechargeable.
    /// </summary>
    private void OnInsertAttempt(EntityUid uid, ChargerComponent component, ContainerIsInsertingAttemptEvent args)
    {
        if (!component.Initialized)
            return;

        if (args.Container.ID != component.SlotId)
            return;

        if (!TryComp<PowerCellSlotComponent>(args.EntityUid, out var cellSlot))
            return;

        if (!cellSlot.FitsInCharger)
            args.Cancel();
    }

    private void OnEntityStorageInsertAttempt(EntityUid uid, ChargerComponent component, ref InsertIntoEntityStorageAttemptEvent args)
    {
        if (!component.Initialized || args.Cancelled)
            return;

        if (!TryComp<PowerCellSlotComponent>(uid, out var cellSlot))
            return;

        if (!cellSlot.FitsInCharger)
            args.Cancelled = true;
    }

    private void OnUpdateStatus(EntityUid uid, ChargerComponent component, ref ChargerUpdateStatusEvent args) // Frontier: Upstream - #28984 End
    {
        UpdateStatus(uid, component);
    }

    private void UpdateStatus(EntityUid uid, ChargerComponent component)
    {
        var status = GetStatus(uid, component);
        TryComp(uid, out AppearanceComponent? appearance);

        if (!_container.TryGetContainer(uid, component.SlotId, out var container))
            return;

        _appearance.SetData(uid, CellVisual.Occupied, container.ContainedEntities.Count != 0, appearance);
        if (component.Status == status || !TryComp(uid, out ApcPowerReceiverComponent? receiver))
            return;

        //if (component.Status == CellChargerStatus.Charging) // Frontier: Upstream - #28984
        //{
        //    AddComp<ActiveChargerComponent>(uid);
        //}
        //else
        //{
        //    RemComp<ActiveChargerComponent>(uid);
        //}

        component.Status = status;

        switch (component.Status)
        {
            case CellChargerStatus.Off:
                receiver.Load = 0;
                _appearance.SetData(uid, CellVisual.Light, CellChargerStatus.Off, appearance);
                break;
            case CellChargerStatus.Empty:
                receiver.Load = 0;
                _appearance.SetData(uid, CellVisual.Light, CellChargerStatus.Empty, appearance);
                break;
            case CellChargerStatus.Charging:
                receiver.Load = component.ChargeRate; //does not scale with multiple slotted batteries
                _appearance.SetData(uid, CellVisual.Light, CellChargerStatus.Charging, appearance);
                break;
            case CellChargerStatus.Charged:
                receiver.Load = 0;
                _appearance.SetData(uid, CellVisual.Light, CellChargerStatus.Charged, appearance);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void OnEmpPulse(EntityUid uid, ChargerComponent component, ref EmpPulseEvent args) // Frontier: Upstream - #28984
    {
        //args.Affected = true;
        //args.Disabled = true;
        // we don't care if we haven't been disabled
        if (!args.Disabled)
            return;

        // if the recharger is hit by an emp pulse,
        // stop recharging contained batteries to save resources
        if (!_container.TryGetContainer(uid, component.SlotId, out var container))
            return;

        foreach (var containedEntity in container.ContainedEntities)
        {
            if (!SearchForBattery(containedEntity, out _, out _))
                continue;

            StopChargingBattery(uid, component, containedEntity);
        }
    }

    private void OnEmpDisabledRemoved(EntityUid uid, ChargerComponent component, ref EmpDisabledRemoved args) // Frontier: Upstream - #28984
    {
        // if an emp disable subsides,
        // attempt to start charging all batteries
        if (!_container.TryGetContainer(uid, component.SlotId, out var container))
            return;

        foreach (var containedEntity in container.ContainedEntities)
        {
            if (!SearchForBattery(containedEntity, out _, out _))
                continue;

            StartChargingBattery(uid, component, containedEntity);
        }
    }

    private CellChargerStatus GetStatus(EntityUid uid, ChargerComponent component) // Frontier: Upstream - #28984
    {
        if (!component.Portable)
        {
            if (!TryComp(uid, out TransformComponent? transformComponent) || !transformComponent.Anchored)
                return CellChargerStatus.Off;
        }

        if (!TryComp(uid, out ApcPowerReceiverComponent? apcPowerReceiverComponent))
            return CellChargerStatus.Off;

        if (!component.Portable && !apcPowerReceiverComponent.Powered)
            return CellChargerStatus.Off;

        if (!_container.TryGetContainer(uid, component.SlotId, out var container))
            return CellChargerStatus.Off;

        if (container.ContainedEntities.Count == 0)
            return CellChargerStatus.Empty;

        var statusOut = CellChargerStatus.Off;

        foreach (var containedEntity in container.ContainedEntities)
        {
            // if none of the slotted items are actually batteries, represent the charger as off
            if (!SearchForBattery(containedEntity, out _, out _))
                continue;

            // if all batteries are either EMP'd or fully charged, represent the charger as fully charged
            statusOut = CellChargerStatus.Charged;
            if (HasComp<EmpDisabledComponent>(containedEntity))
                continue;

            if (!HasComp<ChargingComponent>(containedEntity))
                continue;

            // if we have atleast one battery being charged, represent the charger as charging;
            statusOut = CellChargerStatus.Charging;
            break;
        }

        return statusOut;
    }

    private void TransferPower(EntityUid uid, EntityUid targetEntity, ChargerComponent component, float frameTime)
    {
        if (!TryComp(uid, out ApcPowerReceiverComponent? receiverComponent))
            return;

        if (!receiverComponent.Powered)
            return;

        if (_whitelistSystem.IsWhitelistFail(component.Whitelist, targetEntity))
            return;

        if (!SearchForBattery(targetEntity, out var batteryUid, out var heldBattery))
            return;

        _battery.TrySetCharge(batteryUid.Value, heldBattery.CurrentCharge + component.ChargeRate * frameTime, heldBattery); // Frontier: Upstream - #28984
        // Just so the sprite won't be set to 99.99999% visibility
        if (heldBattery.MaxCharge - heldBattery.CurrentCharge < 0.01)
        {
            _battery.TrySetCharge(batteryUid.Value, heldBattery.MaxCharge, heldBattery); // Frontier: Upstream - #28984
        }

        UpdateStatus(uid, component);
    }

    private bool SearchForBattery(EntityUid uid, [NotNullWhen(true)] out EntityUid? batteryUid, [NotNullWhen(true)] out BatteryComponent? component)
    {
        // try get a battery directly on the inserted entity
        if (!TryComp(uid, out component))
        {
            // or by checking for a power cell slot on the inserted entity
            return _powerCell.TryGetBatteryFromSlot(uid, out batteryUid, out component);
        }
        batteryUid = uid;
        return true;
    }
}

[ByRefEvent] // Frontier: Upstream - #28984
public record struct ChargerUpdateStatusEvent();
