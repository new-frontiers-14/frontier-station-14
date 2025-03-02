using Content.Server.DeviceLinking.Components;
using SignalReceivedEvent = Content.Server.DeviceLinking.Events.SignalReceivedEvent;
using Robust.Shared.Random;
using System.Linq;
using Content.Shared.DeviceLinking;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using static Content.Shared.DeviceLinking.RngDeviceVisuals;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Server.UserInterface;
using Robust.Shared.Prototypes;
using Content.Shared.UserInterface;

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
        SubscribeLocalEvent<RngDeviceComponent, AfterActivatableUIOpenEvent>(OnAfterActivatableUIOpen);
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

    private void UpdateUserInterface(EntityUid uid, RngDeviceComponent comp)
    {
        if (_userInterfaceSystem.HasUi(uid, RngDeviceUiKey.Key))
        {
            _userInterfaceSystem.SetUiState(uid, RngDeviceUiKey.Key, new RngDeviceBoundUserInterfaceState(comp.Muted));
        }
    }

    private void OnSignalReceived(EntityUid uid, RngDeviceComponent comp, ref SignalReceivedEvent args)
    {
        var roll = _random.Next(1, comp.Outputs + 1);
        var outputPort = roll switch
        {
            1 => new ProtoId<SourcePortPrototype>(comp.Output1Port),
            2 => new ProtoId<SourcePortPrototype>(comp.Output2Port),
            3 => new ProtoId<SourcePortPrototype>(comp.Output3Port),
            4 => new ProtoId<SourcePortPrototype>(comp.Output4Port),
            5 => new ProtoId<SourcePortPrototype>(comp.Output5Port),
            6 => new ProtoId<SourcePortPrototype>(comp.Output6Port),
            _ => throw new ArgumentOutOfRangeException($"Invalid roll value: {roll}")
        };

        // Update the appearance to show the current roll
        if (TryComp<AppearanceComponent>(uid, out var appearance))
        {
            var statePrefix = comp.Outputs switch
            {
                2 => "percentile",  // Percentile die
                4 => "d4",         // 4-sided die
                6 => "d6",         // 6-sided die
                _ => throw new ArgumentException($"Unsupported number of outputs: {comp.Outputs}")
            };

            var stateNumber = comp.Outputs == 2 ? roll * 10 : roll;  // For percentile die, use 10/20 instead of 1/2
            _appearance.SetData(uid, RngDeviceVisuals.State, $"{statePrefix}_{stateNumber}", appearance);

            // Play the dice rolling sound if not muted
            if (!comp.Muted)
                _audio.PlayPvs(new SoundCollectionSpecifier("Dice"), uid);
        }

        _deviceLink.InvokePort(uid, outputPort);
    }
}
