using Content.Server.DeviceLinking.Components;
using Content.Shared.DeviceLinking.Events;
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
using Content.Shared._NF.DeviceLinking.Systems;

namespace Content.Server._NF.DeviceLinking.Systems;

public sealed class RngDeviceSystem : SharedRngDeviceSystem
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
        SubscribeLocalEvent<RngDeviceComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<RngDeviceComponent, InteractHandEvent>(OnInteract);
    }

    private void OnInteract(Entity<RngDeviceComponent> ent, ref InteractHandEvent args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        _ui.OpenUi(ent.Owner, RngDeviceUiKey.Key, actor.PlayerSession);
        args.Handled = true;
    }

    private void OnInit(Entity<RngDeviceComponent> ent, ref ComponentInit args)
    {
        // Ensure we have the server component
        var serverComp = EnsureComp<RngDeviceServerComponent>(ent.Owner);
        
        _deviceLink.EnsureSinkPorts(ent.Owner, serverComp.InputPort);

        // Initialize the ports array based on output count
        var ports = new ProtoId<SourcePortPrototype>[ent.Comp.Outputs];
        for (int i = 0; i < ent.Comp.Outputs; i++)
        {
            ports[i] = $"RngOutput{i + 1}";
        }
        _deviceLink.EnsureSourcePorts(ent.Owner, ports);

        // Ensure the state prefix is set in the server component
        if (string.IsNullOrEmpty(serverComp.StatePrefix))
        {
            throw new InvalidOperationException($"StatePrefix not set for RngDevice with {ent.Comp.Outputs} outputs. StatePrefix must be set in the prototype.");
        }
    }

    private void OnSignalReceived(Entity<RngDeviceComponent> ent, ref SignalReceivedEvent args)
    {
        if (!TryComp<RngDeviceServerComponent>(ent.Owner, out var serverComp))
            return;

        var (roll, outputPort) = PerformRoll(ent, serverComp);

        // Update visual state
        UpdateVisualState(ent.Owner, ent.Comp, serverComp, roll);

        // Handle signal output based on mode
        if (ent.Comp.EdgeMode)
            HandleEdgeModeSignals(ent.Owner, ent.Comp, outputPort);
        else
            HandleNormalModeSignal(ent.Owner, outputPort);

        // Component data is automatically networked, no need to call UpdateUiState
        Dirty(ent);
    }

    private (int roll, int outputPort) PerformRoll(Entity<RngDeviceComponent> ent, RngDeviceServerComponent serverComp)
    {
        // Use the shared GenerateRoll method
        var (roll, outputPort) = GenerateRoll(ent.Comp.Outputs, ent.Comp.TargetNumber);

        // Store the values for future reference in the networked component
        ent.Comp.LastRoll = roll;
        ent.Comp.LastOutputPort = outputPort;

        // Play sound if not muted
        if (!ent.Comp.Muted)
            _audio.PlayPredicted(serverComp.Sound, ent.Owner, null);

        return (roll, outputPort);
    }

    private void UpdateVisualState(EntityUid uid, RngDeviceComponent component, RngDeviceServerComponent serverComp, int roll)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        var stateNumber = component.Outputs switch
        {
            2 => roll == 100 ? 0 : (roll / 10) * 10,  // Show "00" for 100, otherwise round down to nearest 10
            10 => roll == 10 ? 0 : roll,  // Show "0" for 10
            _ => roll
        };
        _appearance.SetData(uid, RngDeviceVisuals.State, $"{serverComp.StatePrefix}_{stateNumber}", appearance);
    }

    private void HandleNormalModeSignal(EntityUid uid, int outputPort)
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
        return $"RngOutput{portNumber}";
    }
}
