using System.Linq;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.PowerCell;
using Content.Shared.Medical.CrewMonitoring;
using Content.Shared.Medical.SuitSensor;
using Robust.Server.GameObjects;

namespace Content.Server.Medical.CrewMonitoring
{
    public sealed class CrewMonitoringConsoleSystem : EntitySystem
    {
        [Dependency] private readonly PowerCellSystem _cell = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<CrewMonitoringConsoleComponent, ComponentRemove>(OnRemove);
            SubscribeLocalEvent<CrewMonitoringConsoleComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
            SubscribeLocalEvent<CrewMonitoringConsoleComponent, BoundUIOpenedEvent>(OnUIOpened);
        }

        private void OnRemove(EntityUid uid, CrewMonitoringConsoleComponent component, ComponentRemove args)
        {
            component.ConnectedSensors.Clear();
        }

        private void OnPacketReceived(EntityUid uid, CrewMonitoringConsoleComponent component, DeviceNetworkPacketEvent args)
        {
            var payload = args.Data;
            // check command
            if (!payload.TryGetValue(DeviceNetworkConstants.Command, out string? command))
                return;
            if (command != DeviceNetworkConstants.CmdUpdatedState)
                return;
            if (!payload.TryGetValue(SuitSensorConstants.NET_STATUS_COLLECTION, out Dictionary<string, SuitSensorStatus>? sensorStatus))
                return;

            component.ConnectedSensors = sensorStatus;
            UpdateUserInterface(uid, component);
        }

        private void OnUIOpened(EntityUid uid, CrewMonitoringConsoleComponent component, BoundUIOpenedEvent args)
        {
            if (!_cell.TryUseActivatableCharge(uid))
                return;

            UpdateUserInterface(uid, component);
        }

        private void UpdateUserInterface(EntityUid uid, CrewMonitoringConsoleComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            if (!_uiSystem.TryGetUi(uid, CrewMonitoringUIKey.Key, out var bui))
                return;

            // update all sensors info
            var allSensors = component.ConnectedSensors.Values.ToList();
            _uiSystem.SetUiState(bui, new CrewMonitoringState(allSensors, component.Snap, component.Precision));
        }
    }
}
