using Content.Server._Starlight.Atmos;
using Content.Server._Starlight.DeviceLinking.Components;
using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceNetwork;
using JetBrains.Annotations;

namespace Content.Server._Starlight.DeviceLinking.Systems;

[UsedImplicitly]
public sealed partial class ToggleableSignalSystem : EntitySystem
{
    [Dependency] private DeviceLinkSystem _signalSystem = default!;
    [Dependency] private ToggleableAtmosDeviceSystem _toggleableAtmosDeviceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ToggleableSignalComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<ToggleableSignalComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<ToggleableSignalComponent> entity, ref ComponentInit args) =>
        _signalSystem.EnsureSinkPorts(entity, entity.Comp.OnPort, entity.Comp.OffPort, entity.Comp.TogglePort);

    private void OnSignalReceived(Entity<ToggleableSignalComponent> entity, ref SignalReceivedEvent args)
    {
        if (!TryComp<ToggleableAtmosDeviceComponent>(entity, out var device))
            return;

        var component = new Entity<ToggleableAtmosDeviceComponent?>(entity.Owner, device);

        var state = SignalState.Momentary;
        args.Data?.TryGetValue(DeviceNetworkConstants.LogicState, out state);

        if (state is not (SignalState.High or SignalState.Momentary)) return;
        if (args.Port == entity.Comp.OnPort)
            _toggleableAtmosDeviceSystem.Set(component, true);
        else if (args.Port == entity.Comp.OffPort)
            _toggleableAtmosDeviceSystem.Set(component, false);
        else if (args.Port == entity.Comp.TogglePort)
            _toggleableAtmosDeviceSystem.Toggle(component);
    }
}
