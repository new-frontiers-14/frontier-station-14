using Content.Server.DeviceLinking.Components;
using SignalReceivedEvent = Content.Server.DeviceLinking.Events.SignalReceivedEvent;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Content.Server.UserInterface;
using Robust.Shared.Prototypes;
using Content.Shared.UserInterface;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Components;
using Content.Shared.DeviceNetwork;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceLinking.Systems;
using Content.Shared._NF.DeviceLinking;
using Content.Shared._NF.DeviceLinking.Components;
using SignalState = Content.Shared._NF.DeviceLinking.Components.SignalState;
using Content.Server._NF.DeviceLinking.Components;
using Content.Shared._NF.DeviceLinking.Visuals;
using Robust.Shared.Timing;
using Robust.Shared.GameObjects;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Robust.Shared.Player;
using Robust.Shared.Map;

namespace Content.Server._NF.DeviceLinking.Systems;

public sealed class RngDeviceSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly NetworkPayload _edgeModePayload = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RngDeviceComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<RngDeviceComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RngDeviceComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<RngDeviceComponent, RngDeviceToggleMuteMessage>(OnToggleMute);
        SubscribeLocalEvent<RngDeviceComponent, RngDeviceToggleEdgeModeMessage>(OnToggleEdgeMode);
        SubscribeLocalEvent<RngDeviceComponent, RngDeviceSetTargetNumberMessage>(OnSetTargetNumber);
        SubscribeLocalEvent<RngDeviceComponent, InteractHandEvent>(OnInteract);
        SubscribeLocalEvent<RngDeviceComponent, ExaminedEvent>(OnExamine);
    }

    private void OnInteract(Entity<RngDeviceComponent> ent, ref InteractHandEvent args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        _ui.OpenUi(ent.Owner, RngDeviceUiKey.Key, actor.PlayerSession);
        UpdateUiState(ent.Owner, ent.Comp);
        args.Handled = true;
    }

    private void UpdateUiState(EntityUid uid, RngDeviceComponent component)
    {
        var state = new RngDeviceBoundUserInterfaceState(
            component.LastRoll,
            component.LastOutputPort,
            component.Muted,
            component.EdgeMode,
            component.TargetNumber,
            component.Outputs,
            component.State);

        _ui.SetUiState(uid, RngDeviceUiKey.Key, state);
    }

    private void OnExamine(Entity<RngDeviceComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var comp = ent.Comp;
        args.PushMarkup(Loc.GetString("rng-device-examine-last-roll", ("roll", comp.LastRoll)));

        if (comp.Outputs == 2)  // Only show port info for percentile die
            args.PushMarkup(Loc.GetString("rng-device-examine-last-port", ("port", comp.LastOutputPort)));
    }

    private void OnInit(Entity<RngDeviceComponent> ent, ref ComponentInit args)
    {
        _deviceLink.EnsureSinkPorts(ent.Owner, ent.Comp.InputPort);

        // Initialize the ports array based on output count
        var ports = new ProtoId<SourcePortPrototype>[ent.Comp.Outputs];
        for (int i = 0; i < ent.Comp.Outputs; i++)
        {
            ports[i] = $"RngOutput{i + 1}";
        }
        _deviceLink.EnsureSourcePorts(ent.Owner, ports);

        // Ensure the state prefix is set in the component
        if (string.IsNullOrEmpty(ent.Comp.StatePrefix))
        {
            throw new InvalidOperationException($"StatePrefix not set for RngDevice with {ent.Comp.Outputs} outputs. StatePrefix must be set in the prototype.");
        }
    }

    private void OnMapInit(Entity<RngDeviceComponent> ent, ref MapInitEvent args)
    {
        UpdateUiState(ent.Owner, ent.Comp);
    }

    private void OnSignalReceived(Entity<RngDeviceComponent> ent, ref SignalReceivedEvent args)
    {
        var (roll, outputPort) = PerformRoll(ent.Owner, ent.Comp);

        // Update visual state
        UpdateVisualState(ent.Owner, ent.Comp, roll);

        // Handle signal output based on mode
        if (ent.Comp.EdgeMode)
            HandleEdgeModeSignals(ent.Owner, ent.Comp, outputPort);
        else
            HandleNormalModeSignal(ent.Owner, ent.Comp, outputPort);

        // Update UI state
        UpdateUiState(ent.Owner, ent.Comp);
    }

    private void OnToggleMute(Entity<RngDeviceComponent> ent, ref RngDeviceToggleMuteMessage args)
    {
        ent.Comp.Muted = args.Muted;
        UpdateUiState(ent.Owner, ent.Comp);
    }

    private void OnToggleEdgeMode(Entity<RngDeviceComponent> ent, ref RngDeviceToggleEdgeModeMessage args)
    {
        ent.Comp.EdgeMode = args.EdgeMode;
        UpdateUiState(ent.Owner, ent.Comp);
    }

    private void OnSetTargetNumber(Entity<RngDeviceComponent> ent, ref RngDeviceSetTargetNumberMessage args)
    {
        if (ent.Comp.Outputs != 2)
            return;

        ent.Comp.TargetNumber = Math.Clamp(args.TargetNumber, 1, 100);
        UpdateUiState(ent.Owner, ent.Comp);
    }

    private (int roll, int outputPort) PerformRoll(EntityUid uid, RngDeviceComponent component)
    {
        // Use current tick as seed for deterministic randomness
        var rand = new System.Random((int)_timing.CurTick.Value);

        int roll;
        int outputPort;

        if (component.Outputs == 2)
        {
            // For percentile dice, roll 1-100
            roll = rand.Next(1, 101);
            outputPort = roll <= component.TargetNumber ? 1 : 2;
        }
        else
        {
            roll = rand.Next(1, component.Outputs + 1);
            outputPort = roll;
        }

        // Store the values for future reference
        component.LastRoll = roll;
        component.LastOutputPort = outputPort;

        // Play sound if not muted
        if (!component.Muted)
            _audio.PlayPredicted(component.Sound, uid, null);

        return (roll, outputPort);
    }

    private void UpdateVisualState(EntityUid uid, RngDeviceComponent component, int roll)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        var stateNumber = component.Outputs switch
        {
            2 => roll == 100 ? 0 : (roll / 10) * 10,  // Show "00" for 100, otherwise round down to nearest 10
            10 => roll == 10 ? 0 : roll,  // Show "0" for 10
            _ => roll
        };
        _appearance.SetData(uid, RngDeviceVisuals.State, $"{component.StatePrefix}_{stateNumber}", appearance);
    }

    private void HandleNormalModeSignal(EntityUid uid, RngDeviceComponent component, int outputPort)
    {
        var port = GetOutputPort(uid, outputPort);
        _deviceLink.InvokePort(uid, port);
    }

    private void HandleEdgeModeSignals(EntityUid uid, RngDeviceComponent component, int selectedPort)
    {
        // Set all ports low except the selected one
        for (int i = 1; i <= component.Outputs; i++)
        {
            var port = GetOutputPort(uid, i);
            if (i == selectedPort)
                _deviceLink.InvokePort(uid, port, new NetworkPayload());
            else
                _deviceLink.InvokePort(uid, port, _edgeModePayload);
        }
    }

    // Gets the ProtoId for the specified output port number
    private string GetOutputPort(EntityUid uid, int portNumber)
    {
        if (portNumber < 1 || portNumber > 20)
            throw new ArgumentOutOfRangeException(nameof(portNumber), "Port number must be between 1 and 20");

        return $"RngOutput{portNumber}";
    }
}
