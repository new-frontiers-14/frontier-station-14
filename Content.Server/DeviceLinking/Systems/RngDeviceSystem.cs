using Content.Server.DeviceLinking.Components;
using Content.Server.DeviceNetwork;
using System;
using SignalReceivedEvent = Content.Server.DeviceLinking.Events.SignalReceivedEvent;
using Robust.Shared.Random;
using Content.Shared.Database;
using Content.Server.Administration.Logs;


namespace Content.Server.DeviceLinking.Systems;

public sealed class RngDeviceSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RngDeviceComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<RngDeviceComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    private void OnInit(EntityUid uid, RngDeviceComponent comp, ComponentInit args)
    {

        //_adminLogger.Add(LogType.Action, LogImpact.Low, $"built rng device {ToPrettyString(uid):entity} with nr outputs: {comp.Outputs}");
        _deviceLink.EnsureSinkPorts(uid, comp.InputPort);
        if(comp.Outputs == 2)
        {
            _deviceLink.EnsureSourcePorts(uid, comp.Output1Port, comp.Output2Port);
        }
        else if (comp.Outputs == 4)
        {
            _deviceLink.EnsureSourcePorts(uid, comp.Output1Port, comp.Output2Port, comp.Output3Port, comp.Output4Port);
        }
        else if (comp.Outputs == 6)
        {
            _deviceLink.EnsureSourcePorts(uid, comp.Output1Port, comp.Output2Port, comp.Output3Port, comp.Output4Port, comp.Output5Port, comp.Output6Port);
        }

    }

    private void OnSignalReceived(EntityUid uid, RngDeviceComponent comp, ref SignalReceivedEvent args)
    {
        var roll = _random.Next(1, 3); //TO DO: Configure number of output ports
        if (roll == 1)
        {
            _deviceLink.InvokePort(uid, comp.Output1Port);
        }
        else if (roll == 2)
        {
            _deviceLink.InvokePort(uid, comp.Output2Port);
        }
    }
}
