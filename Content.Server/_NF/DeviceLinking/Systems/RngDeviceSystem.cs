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

    // Pre-allocated arrays for common output counts
    private static ProtoId<SourcePortPrototype>[] InitializeTwoPortsArray(RngDeviceComponent comp) => new[]
    {
        new ProtoId<SourcePortPrototype>(comp.Output1Port),
        new ProtoId<SourcePortPrototype>(comp.Output2Port)
    };

    private static ProtoId<SourcePortPrototype>[] InitializeFourPortsArray(RngDeviceComponent comp) => InitializeTwoPortsArray(comp)
        .Concat(new[]
        {
            new ProtoId<SourcePortPrototype>(comp.Output3Port),
            new ProtoId<SourcePortPrototype>(comp.Output4Port)
        }).ToArray();

    private static ProtoId<SourcePortPrototype>[] InitializeSixPortsArray(RngDeviceComponent comp) => InitializeFourPortsArray(comp)
        .Concat(new[]
        {
            new ProtoId<SourcePortPrototype>(comp.Output5Port),
            new ProtoId<SourcePortPrototype>(comp.Output6Port)
        }).ToArray();

    // Reusable payload for edge mode signals
    private readonly NetworkPayload _edgeModePayload = new();

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

        // Initialize the ports array based on output count
        var ports = InitializePortsArray(comp);
        _deviceLink.EnsureSourcePorts(uid, ports);

        // Cache the state prefix
        comp.StatePrefix = comp.Outputs switch
        {
            2 => "percentile",
            4 => "d4",
            6 => "d6",
            _ => throw new ArgumentException($"Unsupported number of outputs: {comp.Outputs}")
        };

        UpdateUserInterface(uid, comp);
    }

    private ProtoId<SourcePortPrototype>[] InitializePortsArray(RngDeviceComponent comp)
    {
        var array = comp.Outputs switch
        {
            2 => InitializeTwoPortsArray(comp),
            4 => InitializeFourPortsArray(comp),
            6 => InitializeSixPortsArray(comp),
            _ => throw new ArgumentException($"Unsupported number of outputs: {comp.Outputs}")
        };

        comp.PortsArray = array;
        return array;
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

        var stateNumber = comp.Outputs == 2
            ? roll == 100 ? 0 : (roll / 10) * 10  // Show "00" for 100, otherwise round down to nearest 10
            : roll;
        _appearance.SetData(uid, State, $"{comp.StatePrefix}_{stateNumber}", appearance);

        if (!comp.Muted)
            _audio.PlayPvs(new SoundCollectionSpecifier("Dice"), uid);
    }

    private void HandleNormalModeSignal(EntityUid uid, RngDeviceComponent comp, int outputPort)
    {
        var port = GetOutputPort(comp, outputPort);
        _deviceLink.InvokePort(uid, port);
    }

    private void HandleEdgeModeSignals(EntityUid uid, RngDeviceComponent comp, int selectedPort)
    {
        var ports = comp.PortsArray;
        if (ports == null)
            return;

        // Send High signal to selected port and Low signals to others
        for (int i = 0; i < ports.Length; i++)
        {
            var state = (i + 1) == selectedPort ? SignalState.High : SignalState.Low;
            _edgeModePayload.Clear();
            _edgeModePayload.Add(DeviceNetworkConstants.LogicState, state);
            _deviceLink.InvokePort(uid, ports[i], _edgeModePayload);
        }
    }

    /// <summary>
    /// Gets the ProtoId for the specified output port number
    /// </summary>
    /// <param name="comp">The RNG device component</param>
    /// <param name="portNumber">The port number (1-6)</param>
    /// <returns>The ProtoId for the corresponding output port</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when port number is invalid</exception>
    private static ProtoId<SourcePortPrototype> GetOutputPort(RngDeviceComponent comp, int portNumber)
    {
        var portName = portNumber switch
        {
            1 => comp.Output1Port,
            2 => comp.Output2Port,
            3 => comp.Output3Port,
            4 => comp.Output4Port,
            5 => comp.Output5Port,
            6 => comp.Output6Port,
            _ => throw new ArgumentOutOfRangeException(nameof(portNumber), $"Invalid output port number: {portNumber}")
        };
        return new ProtoId<SourcePortPrototype>(portName);
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
