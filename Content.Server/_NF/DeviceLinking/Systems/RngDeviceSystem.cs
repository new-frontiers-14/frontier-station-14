using Content.Server.DeviceLinking.Components;
using SignalReceivedEvent = Content.Server.DeviceLinking.Events.SignalReceivedEvent;
using Robust.Shared.Random;
using System.Linq;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Server.UserInterface;
using Robust.Shared.Prototypes;
using Content.Shared.UserInterface;
using Content.Shared._NF.DeviceLinking;
using static Content.Shared._NF.DeviceLinking.RngDeviceVisuals;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Components;
using Content.Shared.Examine;
using Content.Shared.DeviceNetwork;
using Content.Server.DeviceNetwork;

namespace Content.Server.DeviceLinking.Systems;

public sealed class RngDeviceSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RngDeviceComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<RngDeviceComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<RngDeviceComponent, RngDeviceToggleMuteMessage>(OnToggleMute);
        SubscribeLocalEvent<RngDeviceComponent, RngDeviceToggleEdgeModeMessage>(OnToggleEdgeMode);
        SubscribeLocalEvent<RngDeviceComponent, RngDeviceSetTargetNumberMessage>(OnSetTargetNumber);
        SubscribeLocalEvent<RngDeviceComponent, AfterActivatableUIOpenEvent>(OnAfterActivatableUIOpen);
        SubscribeLocalEvent<RngDeviceComponent, ExaminedEvent>(OnExamined);
    }

    private void OnInit(EntityUid uid, RngDeviceComponent comp, ComponentInit args)
    {
        _deviceLink.EnsureSinkPorts(uid, new ProtoId<SinkPortPrototype>(comp.InputPort));

        // Get the appropriate number of output ports based on comp.Outputs
        var ports = comp.Outputs switch
        {
            2 => new[] { new ProtoId<SourcePortPrototype>(comp.Output1Port), new ProtoId<SourcePortPrototype>(comp.Output2Port) },
            4 => new[] { new ProtoId<SourcePortPrototype>(comp.Output1Port), new ProtoId<SourcePortPrototype>(comp.Output2Port),
                        new ProtoId<SourcePortPrototype>(comp.Output3Port), new ProtoId<SourcePortPrototype>(comp.Output4Port) },
            6 => new[] { new ProtoId<SourcePortPrototype>(comp.Output1Port), new ProtoId<SourcePortPrototype>(comp.Output2Port),
                        new ProtoId<SourcePortPrototype>(comp.Output3Port), new ProtoId<SourcePortPrototype>(comp.Output4Port),
                        new ProtoId<SourcePortPrototype>(comp.Output5Port), new ProtoId<SourcePortPrototype>(comp.Output6Port) },
            _ => throw new ArgumentException($"Unsupported number of outputs: {comp.Outputs}")
        };

        _deviceLink.EnsureSourcePorts(uid, ports);

        // Setup UI
        UpdateUserInterface(uid, comp);
    }

    private void OnAfterActivatableUIOpen(EntityUid uid, RngDeviceComponent comp, AfterActivatableUIOpenEvent args)
    {
        UpdateUserInterface(uid, comp);
    }

    private void OnToggleMute(EntityUid uid, RngDeviceComponent comp, RngDeviceToggleMuteMessage args)
    {
        comp.Muted = args.Muted;
        UpdateUserInterface(uid, comp);
    }

    private void OnToggleEdgeMode(EntityUid uid, RngDeviceComponent comp, RngDeviceToggleEdgeModeMessage args)
    {
        comp.EdgeMode = args.EdgeMode;
        UpdateUserInterface(uid, comp);
    }

    private void OnSetTargetNumber(EntityUid uid, RngDeviceComponent comp, RngDeviceSetTargetNumberMessage args)
    {
        if (comp.Outputs != 2)
            return;

        comp.TargetNumber = Math.Clamp(args.TargetNumber, 1, 100);
        UpdateUserInterface(uid, comp);
    }

    private void UpdateUserInterface(EntityUid uid, RngDeviceComponent comp)
    {
        if (_userInterfaceSystem.HasUi(uid, RngDeviceUiKey.Key))
        {
            _userInterfaceSystem.SetUiState(uid, RngDeviceUiKey.Key, new RngDeviceBoundUserInterfaceState(comp.Muted, comp.TargetNumber, comp.Outputs, comp.EdgeMode));
        }
    }

    private void OnSignalReceived(EntityUid uid, RngDeviceComponent comp, ref SignalReceivedEvent args)
    {
        // Roll the dice and determine output port
        int roll;
        int outputPort;
        if (comp.Outputs == 2)
        {
            // For percentile dice, roll 1-100 and determine which output to trigger based on target number
            roll = _random.Next(1, 101);
            outputPort = roll <= comp.TargetNumber ? 1 : 2;
        }
        else
        {
            roll = _random.Next(1, comp.Outputs + 1);
            outputPort = roll;
        }

        // Store the values for future use
        comp.LastRoll = roll;
        comp.LastOutputPort = outputPort;

        // Update visual state and play sound
        UpdateVisualState(uid, comp, roll);

        // Handle signal output based on mode
        if (comp.EdgeMode)
            HandleEdgeModeSignals(uid, comp, outputPort);
        else
            HandleNormalModeSignal(uid, comp, outputPort);
    }

    private void UpdateVisualState(EntityUid uid, RngDeviceComponent comp, int roll)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        var statePrefix = comp.Outputs switch
        {
            2 => "percentile",  // Percentile die
            4 => "d4",         // 4-sided die
            6 => "d6",         // 6-sided die
            _ => throw new ArgumentException($"Unsupported number of outputs: {comp.Outputs}")
        };

        var stateNumber = comp.Outputs == 2
            ? ((roll - 1) / 10) * 10  // Round down to nearest 10 for percentile die
            : roll;
        _appearance.SetData(uid, State, $"{statePrefix}_{stateNumber}", appearance);

        // Play the dice rolling sound if not muted
        if (!comp.Muted)
            _audio.PlayPvs(new SoundCollectionSpecifier("Dice"), uid);
    }

    private void HandleNormalModeSignal(EntityUid uid, RngDeviceComponent comp, int outputPort)
    {
        var port = outputPort switch
        {
            1 => new ProtoId<SourcePortPrototype>(comp.Output1Port),
            2 => new ProtoId<SourcePortPrototype>(comp.Output2Port),
            3 => new ProtoId<SourcePortPrototype>(comp.Output3Port),
            4 => new ProtoId<SourcePortPrototype>(comp.Output4Port),
            5 => new ProtoId<SourcePortPrototype>(comp.Output5Port),
            6 => new ProtoId<SourcePortPrototype>(comp.Output6Port),
            _ => throw new ArgumentOutOfRangeException($"Invalid output port number: {outputPort}")
        };

        // Send momentary signal to the selected port
        _deviceLink.InvokePort(uid, port);
    }

    private void HandleEdgeModeSignals(EntityUid uid, RngDeviceComponent comp, int selectedPort)
    {
        // Get all output ports
        var allPorts = new List<ProtoId<SourcePortPrototype>>();
        for (int i = 1; i <= comp.Outputs; i++)
        {
            var port = i switch
            {
                1 => new ProtoId<SourcePortPrototype>(comp.Output1Port),
                2 => new ProtoId<SourcePortPrototype>(comp.Output2Port),
                3 => new ProtoId<SourcePortPrototype>(comp.Output3Port),
                4 => new ProtoId<SourcePortPrototype>(comp.Output4Port),
                5 => new ProtoId<SourcePortPrototype>(comp.Output5Port),
                6 => new ProtoId<SourcePortPrototype>(comp.Output6Port),
                _ => throw new ArgumentOutOfRangeException($"Invalid output port number: {i}")
            };
            allPorts.Add(port);
        }

        // Send High signal to selected port and Low signals to others
        foreach (var port in allPorts)
        {
            var state = port == allPorts[selectedPort - 1] ? SignalState.High : SignalState.Low;
            var payload = new NetworkPayload
            {
                { DeviceNetworkConstants.LogicState, state }
            };
            _deviceLink.InvokePort(uid, port, payload);
        }
    }

    private void OnExamined(EntityUid uid, RngDeviceComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("rng-device-examine-last-roll", ("roll", component.LastRoll)));

        if (component.Outputs == 2)  // Only show port info for percentile die
            args.PushMarkup(Loc.GetString("rng-device-examine-last-port", ("port", component.LastOutputPort)));
    }
}
