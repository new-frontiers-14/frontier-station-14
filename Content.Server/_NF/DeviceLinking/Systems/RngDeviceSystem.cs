using Content.Server.DeviceLinking.Components;
using SignalReceivedEvent = Content.Server.DeviceLinking.Events.SignalReceivedEvent;
using Robust.Shared.Random;
using System.Linq;
using Content.Shared.DeviceLinking;

namespace Content.Server.DeviceLinking.Systems;

public sealed class RngDeviceSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RngDeviceComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<RngDeviceComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    private void OnInit(EntityUid uid, RngDeviceComponent comp, ComponentInit args)
    {
        _deviceLink.EnsureSinkPorts(uid, comp.InputPort);

        // Get the appropriate number of output ports based on comp.Outputs
        var ports = comp.Outputs switch
        {
            2 => new[] { comp.Output1Port, comp.Output2Port },
            4 => new[] { comp.Output1Port, comp.Output2Port, comp.Output3Port, comp.Output4Port },
            6 => new[] { comp.Output1Port, comp.Output2Port, comp.Output3Port, comp.Output4Port, comp.Output5Port, comp.Output6Port },
            _ => throw new ArgumentException($"Unsupported number of outputs: {comp.Outputs}")
        };

        _deviceLink.EnsureSourcePorts(uid, ports);
    }

    private void OnSignalReceived(EntityUid uid, RngDeviceComponent comp, ref SignalReceivedEvent args)
    {
        var roll = _random.Next(1, comp.Outputs + 1);
        var outputPort = roll switch
        {
            1 => comp.Output1Port,
            2 => comp.Output2Port,
            3 => comp.Output3Port,
            4 => comp.Output4Port,
            5 => comp.Output5Port,
            6 => comp.Output6Port,
            _ => throw new ArgumentOutOfRangeException($"Invalid roll value: {roll}")
        };
        
        _deviceLink.InvokePort(uid, outputPort);
    }
}
