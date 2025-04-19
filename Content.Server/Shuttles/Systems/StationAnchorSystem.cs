using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Server.Shuttles.Components;
using Content.Shared.Construction.Components;
using Content.Shared.Popups;
using Content.Server.DeviceLinking.Events; // Frontier
using Content.Server.DeviceLinking.Systems; // Frontier
using Content.Server.DeviceNetwork; // Frontier
using Content.Server.DeviceNetwork.Systems; // Frontier
using Content.Server.Power.Components; // Frontier

namespace Content.Server.Shuttles.Systems;

public sealed class StationAnchorSystem : EntitySystem
{
    [Dependency] private readonly ShuttleSystem _shuttleSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly DeviceLinkSystem _signalSystem = default!; // Frontier
    [Dependency] private readonly PowerChargeSystem _chargeSystem = default!; // Frontier

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StationAnchorComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
        SubscribeLocalEvent<StationAnchorComponent, AnchorStateChangedEvent>(OnAnchorStationChange);

        SubscribeLocalEvent<StationAnchorComponent, ChargedMachineActivatedEvent>(OnActivated);
        SubscribeLocalEvent<StationAnchorComponent, ChargedMachineDeactivatedEvent>(OnDeactivated);

        SubscribeLocalEvent<StationAnchorComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<StationAnchorComponent, ComponentInit>(OnInit); // Frontier
        SubscribeLocalEvent<StationAnchorComponent, SignalReceivedEvent>(OnSignalReceived); // Frontier
        SubscribeLocalEvent<StationAnchorComponent, DeviceNetworkPacketEvent>(OnPacketReceived); // Frontier
    }

    private void OnMapInit(Entity<StationAnchorComponent> ent, ref MapInitEvent args)
    {
        if (!ent.Comp.SwitchedOn)
            return;

        SetStatus(ent, true);
    }

    private void OnActivated(Entity<StationAnchorComponent> ent, ref ChargedMachineActivatedEvent args)
    {
        SetStatus(ent, true);
    }

    private void OnDeactivated(Entity<StationAnchorComponent> ent, ref ChargedMachineDeactivatedEvent args)
    {
        SetStatus(ent, false);
    }

    /// <summary>
    /// Prevent unanchoring when anchor is active
    /// </summary>
    private void OnUnanchorAttempt(Entity<StationAnchorComponent> ent, ref UnanchorAttemptEvent args)
    {
        if (!ent.Comp.SwitchedOn)
            return;

        _popupSystem.PopupEntity(
            Loc.GetString("station-anchor-unanchoring-failed"),
            ent,
            args.User,
            PopupType.Medium);

        args.Cancel();
    }

    private void OnAnchorStationChange(Entity<StationAnchorComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            SetStatus(ent, false);
    }

    // Frontier: anchor device linking
    private void OnInit(EntityUid uid, StationAnchorComponent anchor, ComponentInit args)
    {
        _signalSystem.EnsureSinkPorts(uid, anchor.OnPort, anchor.OffPort, anchor.TogglePort);
    }

    private void OnPacketReceived(EntityUid uid, StationAnchorComponent component, DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? command) ||
            command != DeviceNetworkConstants.CmdSetState)
            return;
        if (!args.Data.TryGetValue(DeviceNetworkConstants.StateEnabled, out bool enabled))
            return;

        SetAnchorPower((uid, component), enabled);
    }

    private void OnSignalReceived(EntityUid uid, StationAnchorComponent component, ref SignalReceivedEvent args)
    {
        if (args.Port == component.OffPort)
            SetAnchorPower((uid, component), false);
        else if (args.Port == component.OnPort)
            SetAnchorPower((uid, component), true);
        else if (args.Port == component.TogglePort)
            ToggleAnchorPower((uid, component));
    }

    private void SetAnchorPower(Entity<StationAnchorComponent> ent, bool value)
    {
        if (TryComp<PowerChargeComponent>(ent, out var entPowerHandler))
            _chargeSystem.SetSwitchedOn(ent, entPowerHandler, value);
    }

    private void ToggleAnchorPower(Entity<StationAnchorComponent> ent)
    {
        if (TryComp<PowerChargeComponent>(ent, out var entPowerHandler))
            _chargeSystem.SetSwitchedOn(ent, entPowerHandler, !entPowerHandler.SwitchedOn);
    }
    // End Frontier: anchor device linking

    private void SetStatus(Entity<StationAnchorComponent> ent, bool enabled, ShuttleComponent? shuttleComponent = default)
    {
        var transform = Transform(ent);
        var grid = transform.GridUid;
        if (!grid.HasValue || !transform.Anchored && enabled || !Resolve(grid.Value, ref shuttleComponent))
            return;

        if (enabled)
        {
            _shuttleSystem.Disable(grid.Value);
        }
        else
        {
            _shuttleSystem.Enable(grid.Value);
        }

        shuttleComponent.Enabled = !enabled;
        ent.Comp.SwitchedOn = enabled;
    }
}
