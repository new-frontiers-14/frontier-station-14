using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using JetBrains.Annotations;
using Robust.Shared.Timing;

namespace Content.Server.Atmos.Piping.EntitySystems
{
    [UsedImplicitly]
    public sealed class AtmosDeviceSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;

        private float _timer;

        // Set of atmos devices that are off-grid but have JoinSystem set.
        private readonly HashSet<Entity<AtmosDeviceComponent>> _joinedDevices = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<AtmosDeviceComponent, ComponentInit>(OnDeviceInitialize);
            SubscribeLocalEvent<AtmosDeviceComponent, ComponentShutdown>(OnDeviceShutdown);
            // Re-anchoring should be handled by the parent change.
            SubscribeLocalEvent<AtmosDeviceComponent, EntParentChangedMessage>(OnDeviceParentChanged);
            SubscribeLocalEvent<AtmosDeviceComponent, AnchorStateChangedEvent>(OnDeviceAnchorChanged);
        }

        public void JoinAtmosphere(Entity<AtmosDeviceComponent> ent)
        {
            var component = ent.Comp;
            var transform = Transform(ent);

            if (component.RequireAnchored && !transform.Anchored)
                return;

            // Attempt to add device to a grid atmosphere.
            bool onGrid = (transform.GridUid != null) && _atmosphereSystem.AddAtmosDevice(transform.GridUid!.Value, component);

            if (!onGrid && component.JoinSystem)
            {
                _joinedDevices.Add(ent);
                component.JoinedSystem = true;
            }

            component.LastProcess = _gameTiming.CurTime;
            RaiseLocalEvent(ent, new AtmosDeviceEnabledEvent());
        }

        public void LeaveAtmosphere(Entity<AtmosDeviceComponent> ent)
        {
            var component = ent.Comp;
            // Try to remove the component from an atmosphere, and if not
            if (component.JoinedGrid != null && !_atmosphereSystem.RemoveAtmosDevice(component.JoinedGrid.Value, component))
            {
                // The grid might have been removed but not us... This usually shouldn't happen.
                component.JoinedGrid = null;
                return;
            }

            if (component.JoinedSystem)
            {
                _joinedDevices.Remove(ent);
                component.JoinedSystem = false;
            }

            component.LastProcess = TimeSpan.Zero;
            RaiseLocalEvent(ent, new AtmosDeviceDisabledEvent());
        }

        public void RejoinAtmosphere(Entity<AtmosDeviceComponent> component)
        {
            LeaveAtmosphere(component);
            JoinAtmosphere(component);
        }

        private void OnDeviceInitialize(Entity<AtmosDeviceComponent> ent, ref ComponentInit args)
        {
            JoinAtmosphere(ent);
        }

        private void OnDeviceShutdown(Entity<AtmosDeviceComponent> ent, ref ComponentShutdown args)
        {
            LeaveAtmosphere(ent);
        }

        private void OnDeviceAnchorChanged(Entity<AtmosDeviceComponent> ent, ref AnchorStateChangedEvent args)
        {
            // Do nothing if the component doesn't require being anchored to function.
            if (!ent.Comp.RequireAnchored)
                return;

            if (args.Anchored)
                JoinAtmosphere(ent);
            else
                LeaveAtmosphere(ent);
        }

        private void OnDeviceParentChanged(Entity<AtmosDeviceComponent> ent, ref EntParentChangedMessage args)
        {
            RejoinAtmosphere(ent);
        }

        /// <summary>
        /// Update atmos devices that are off-grid but have JoinSystem set. For devices updates when
        /// a device is on a grid, see AtmosphereSystem:UpdateProcessing().
        /// </summary>
        public override void Update(float frameTime)
        {
            _timer += frameTime;

            if (_timer < _atmosphereSystem.AtmosTime)
                return;

            _timer -= _atmosphereSystem.AtmosTime;

            var time = _gameTiming.CurTime;
            foreach (var device in _joinedDevices)
            {
                RaiseLocalEvent(device, new AtmosDeviceUpdateEvent(_atmosphereSystem.AtmosTime));
                device.Comp.LastProcess = time;
            }
        }
    }
}
