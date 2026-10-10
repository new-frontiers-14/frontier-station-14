using Content.Server._Starlight.Atmos.Piping.Unary.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Audio;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.EntitySystems;
using Content.Shared._Starlight.Atmos.Piping.Unary.Visuals;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping.Components;
using Content.Shared.Interaction;
using Content.Shared.Power;
using JetBrains.Annotations;
using Robust.Server.GameObjects;

namespace Content.Server._Starlight.Atmos.Piping.Unary.EntitySystems;

[UsedImplicitly]
public sealed partial class GasInletSiphonSystem : EntitySystem
{
    [Dependency] private AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private NodeContainerSystem _nodeContainer = default!;
    [Dependency] private PowerReceiverSystem _powerReceiverSystem = default!;
    [Dependency] private TransformSystem _transformSystem = default!;
    [Dependency] private AmbientSoundSystem _ambientSoundSystem = default!;
    [Dependency] private EntityManager _entityManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GasInletSiphonComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<GasInletSiphonComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<GasInletSiphonComponent, AtmosDeviceEnabledEvent>(OnGasInletSiphonEnterAtmosphere);
        SubscribeLocalEvent<GasInletSiphonComponent, AtmosDeviceDisabledEvent>(OnGasInletSiphonLeaveAtmosphere);
        SubscribeLocalEvent<GasInletSiphonComponent, AtmosDeviceUpdateEvent>(OnAirSiphonUpdated);
        SubscribeLocalEvent<GasInletSiphonComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<ToggleableAtmosDeviceComponent, SetToggleSignalReceivedEvent>(OnSetToggleSignalReceived);
        SubscribeLocalEvent<ToggleableAtmosDeviceComponent, ToggleSignalReceivedEvent>(OnToggleSignalReceived);
    }

    private void OnMapInit(Entity<GasInletSiphonComponent> entity, ref MapInitEvent args) => UpdateState(entity, entity.Comp);

    private void OnActivate(Entity<GasInletSiphonComponent> entity, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        Set(entity, entity.Comp, !entity.Comp.Enabled);
        args.Handled = true;
    }

    private void OnSetToggleSignalReceived(Entity<ToggleableAtmosDeviceComponent> entity, ref SetToggleSignalReceivedEvent args)
    {
        if (!_entityManager.TryGetComponent<GasInletSiphonComponent>(entity, out var device))
            return;

        var siphonEntity = new Entity<GasInletSiphonComponent>(entity.Owner, device);
        Set(siphonEntity, device, args.Value);
    }

    private void OnToggleSignalReceived(Entity<ToggleableAtmosDeviceComponent> entity, ref ToggleSignalReceivedEvent args)
    {
        if (!_entityManager.TryGetComponent<GasInletSiphonComponent>(entity, out var device))
            return;

        var siphonEntity = new Entity<GasInletSiphonComponent>(entity.Owner, device);
        Set(siphonEntity, device, !device.Enabled);
    }

    private void OnGasInletSiphonEnterAtmosphere(Entity<GasInletSiphonComponent> entity, ref AtmosDeviceEnabledEvent args) => UpdateState(entity, entity.Comp);

    private void OnGasInletSiphonLeaveAtmosphere(Entity<GasInletSiphonComponent> entity, ref AtmosDeviceDisabledEvent args) => UpdateState(entity, entity.Comp);

    private void OnAirSiphonUpdated(Entity<GasInletSiphonComponent> entity, ref AtmosDeviceUpdateEvent args)
    {
        if (!_powerReceiverSystem.IsPowered(entity))
            return;

        var siphon = entity.Comp;

        if (!siphon.Enabled || !_nodeContainer.TryGetNode(entity.Owner, siphon.OutletName, out PipeNode? outlet))
            return;

        if (args.Grid is not {} grid)
            return;

        var position = _transformSystem.GetGridTilePositionOrDefault(entity.Owner);
        var environment = _atmosphereSystem.GetTileMixture(grid, args.Map, position, true);

        Scrub(args.dt, siphon, environment, outlet.Air);

        var enumerator = _atmosphereSystem.GetAdjacentTileMixtures(grid, position, false, true);
        while (enumerator.MoveNext(out var adjacent))
        {
            Scrub(args.dt, siphon, adjacent, outlet.Air);
        }
    }

    private void Scrub(float deltaTime, GasInletSiphonComponent siphon, GasMixture? tile, GasMixture destination)
    {
        if (tile == null
            || destination.Pressure >= siphon.MaxPressure)
            return;

        var ratio = MathF.Min(1f, deltaTime * siphon.TransferRate*_atmosphereSystem.PumpSpeedup() / tile.Volume);
        var removed = tile.RemoveRatio(ratio);

        if (MathHelper.CloseToPercent(removed.TotalMoles, 0f))
            return;

        _atmosphereSystem.Merge(destination, removed);
    }

    private void OnPowerChanged(Entity<GasInletSiphonComponent> entity, ref PowerChangedEvent args) => UpdateState(entity, entity.Comp);

    private void UpdateState(Entity<GasInletSiphonComponent> entity, GasInletSiphonComponent siphon, AppearanceComponent? appearance = null)
    {
        if (!Resolve(entity, ref appearance, false))
            return;

        var enabled = siphon.Enabled;
        var powered = _powerReceiverSystem.IsPowered(entity);
        var state = (enabled ? 2 : 0) + (powered ? 1 : 0);
        _appearance.SetData(entity, GasInletSiphonVisuals.State, (GasInletSiphonState)state);
        _ambientSoundSystem.SetAmbience(entity, enabled && powered);
    }

    private void Set(Entity<GasInletSiphonComponent> entity, GasInletSiphonComponent siphon, bool value)
    {
        if (siphon.Enabled == value)
            return;

        siphon.Enabled = value;
        UpdateState(entity, siphon);
    }
}
