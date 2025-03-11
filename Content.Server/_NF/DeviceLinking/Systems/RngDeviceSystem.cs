using Content.Server.DeviceLinking.Components;
using SignalReceivedEvent = Content.Server.DeviceLinking.Events.SignalReceivedEvent;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Content.Server.UserInterface;
using Robust.Shared.Prototypes;
using Content.Shared.UserInterface;
using Content.Shared._NF.DeviceLinking;
using static Content.Shared._NF.DeviceLinking.RngDeviceVisuals;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Components;
using Content.Shared.DeviceNetwork;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceLinking.Systems;
using Content.Shared._NF.DeviceLinking.Systems;
using Content.Shared._NF.DeviceLinking.Components;
using SignalState = Content.Shared._NF.DeviceLinking.Components.SignalState;

namespace Content.Server._NF.DeviceLinking.Systems;

public sealed class RngDeviceSystem : SharedRngDeviceSystem
{
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;

    private readonly NetworkPayload _edgeModePayload = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RngDeviceComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<RngDeviceComponent, RngDeviceToggleMuteMessage>(OnToggleMute);
        SubscribeLocalEvent<RngDeviceComponent, RngDeviceToggleEdgeModeMessage>(OnToggleEdgeMode);
        SubscribeLocalEvent<RngDeviceComponent, RngDeviceSetTargetNumberMessage>(OnSetTargetNumber);
        SubscribeLocalEvent<RngDeviceComponent, AfterActivatableUIOpenEvent>(OnAfterActivatableUIOpen);
    }

    protected override void OnInit(Entity<RngDeviceComponent> ent, ref ComponentInit args)
    {
        base.OnInit(ent, ref args);

        var comp = ent.Comp;
        var uid = ent.Owner;

        // Initialize output ports if they're not already set
        for (int i = 1; i <= 20; i++)
        {
            if (!comp.OutputPorts.ContainsKey(i))
                comp.OutputPorts[i] = $"RngOutput{i}";
        }

        _deviceLink.EnsureSinkPorts(uid, comp.InputPort);

        // Initialize the ports array based on output count
        var ports = CreatePortsArray(comp, comp.Outputs);
        _deviceLink.EnsureSourcePorts(uid, ports);

        UpdateUserInterface(ent);
    }

    private void OnAfterActivatableUIOpen(Entity<RngDeviceComponent> ent, ref AfterActivatableUIOpenEvent args)
    {
        UpdateUserInterface(ent);
    }

    private void OnToggleMute(Entity<RngDeviceComponent> ent, ref RngDeviceToggleMuteMessage args)
    {
        ent.Comp.Muted = args.Muted;
        Dirty(ent);
        UpdateUserInterface(ent);
    }

    private void OnToggleEdgeMode(Entity<RngDeviceComponent> ent, ref RngDeviceToggleEdgeModeMessage args)
    {
        ent.Comp.EdgeMode = args.EdgeMode;
        Dirty(ent);
        UpdateUserInterface(ent);
    }

    private void OnSetTargetNumber(Entity<RngDeviceComponent> ent, ref RngDeviceSetTargetNumberMessage args)
    {
        var comp = ent.Comp;

        if (comp.Outputs != 2)
            return;

        // Update the target number
        comp.TargetNumber = Math.Clamp(args.TargetNumber, 1, 100);

        Dirty(ent);
        UpdateUserInterface(ent);
    }

    private void UpdateUserInterface(Entity<RngDeviceComponent> ent)
    {
        var comp = ent.Comp;
        var uid = ent.Owner;

        if (!_userInterfaceSystem.HasUi(uid, RngDeviceUiKey.Key))
            return;

        _userInterfaceSystem.SetUiState(uid, RngDeviceUiKey.Key,
            new RngDeviceBoundUserInterfaceState(comp.Muted, comp.TargetNumber, comp.Outputs, comp.EdgeMode, GetDeviceType(uid, comp)));
    }

    private void OnSignalReceived(Entity<RngDeviceComponent> ent, ref SignalReceivedEvent args)
    {
        var comp = ent.Comp;
        var uid = ent.Owner;

        // Use the shared roll system for deterministic rolls
        var (roll, outputPort) = PerformRoll(ent);

        // Update visual state
        UpdateVisualState(ent, roll);

        // Handle signal output based on mode
        if (comp.EdgeMode)
            HandleEdgeModeSignals(ent, outputPort);
        else
            HandleNormalModeSignal(ent, outputPort);

        // Dirty the component to ensure changes are synchronized
        Dirty(ent);
    }

    private void UpdateVisualState(Entity<RngDeviceComponent> ent, int roll)
    {
        var comp = ent.Comp;
        var uid = ent.Owner;

        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        var stateNumber = comp.Outputs switch
        {
            2 => roll == 100 ? 0 : (roll / 10) * 10,  // Show "00" for 100, otherwise round down to nearest 10
            10 => roll == 10 ? 0 : roll,  // Show "0" for 10
            _ => roll
        };
        _appearance.SetData(uid, State, $"{comp.StatePrefix}_{stateNumber}", appearance);
    }

    protected override void UpdateVisuals(Entity<RngDeviceComponent> ent)
    {
        UpdateVisualState(ent, ent.Comp.LastRoll);
    }

    private void HandleNormalModeSignal(Entity<RngDeviceComponent> ent, int outputPort)
    {
        var port = GetOutputPort(ent.Comp, outputPort);
        _deviceLink.InvokePort(ent.Owner, port);
    }

    private void HandleEdgeModeSignals(Entity<RngDeviceComponent> ent, int selectedPort)
    {
        var comp = ent.Comp;
        var uid = ent.Owner;
        var ports = CreatePortsArray(comp, comp.Outputs);

        // Clear the payload once before the loop
        _edgeModePayload.Clear();

        // Send High signal to selected port
        _edgeModePayload.Add(DeviceNetworkConstants.LogicState, SignalState.High);
        _deviceLink.InvokePort(uid, ports[selectedPort - 1], _edgeModePayload);

        // Send Low signals to other ports
        _edgeModePayload.Clear();
        _edgeModePayload.Add(DeviceNetworkConstants.LogicState, SignalState.Low);

        for (int i = 0; i < ports.Length; i++)
        {
            if (i + 1 == selectedPort)
                continue;

            _deviceLink.InvokePort(uid, ports[i], _edgeModePayload);
        }
    }
}
