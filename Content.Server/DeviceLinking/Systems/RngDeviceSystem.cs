using Content.Server.DeviceLinking.Components;
using Content.Server.DeviceNetwork;
using System;
using SignalReceivedEvent = Content.Server.DeviceLinking.Events.SignalReceivedEvent;
using Robust.Shared.Random;


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
        _deviceLink.EnsureSourcePorts(uid, comp.OutputHighPort, comp.OutputLowPort);
    }

    private void OnSignalReceived(EntityUid uid, RngDeviceComponent comp, ref SignalReceivedEvent args)
    {
        var roll = _random.Next(1, 3); //TO DO: Configure number of output ports
        if (roll == 1)
        {
            _deviceLink.InvokePort(uid, comp.OutputLowPort);
        }
        else if (roll == 2)
        {
            _deviceLink.InvokePort(uid, comp.OutputHighPort);
        }
    }
}
