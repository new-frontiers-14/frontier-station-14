using Content.Server._NF.Atmos.Components;
using Content.Server.Atmos.Piping.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Shared.Atmos;
using Content.Shared.Construction.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._NF.Atmos.EntitySystems;

// System for handling gas deposits and machines for extracting from gas deposits
public sealed class GasDepositSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly PrototypeManager _prototype = default!;
    [Dependency] private readonly RobustRandom _random = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly AmbientSoundSystem _ambientSound = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomGasDepositComponent, ComponentInit>(OnDepositInit);

        SubscribeLocalEvent<GasDepositExtractorComponent, AnchorAttemptEvent>(OnAnchorAttempt);
        SubscribeLocalEvent<GasDepositExtractorComponent, AnchorStateChangedEvent>(OnAnchorChanged);
    }

    public void OnAnchorAttempt(EntityUid uid, GasDepositExtractorComponent component, AnchorAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp(uid, out TransformComponent? xform)
            || xform.GridUid is not { Valid: true } grid
            || !TryComp(uid, out MapGridComponent? gridComp))
        {
            args.Cancel();
            return;
        }

        var indices = _map.TileIndicesFor(grid, gridComp, xform.Coordinates);
        var enumerator = _map.GetAnchoredEntitiesEnumerator(grid, gridComp, indices);

        while (enumerator.MoveNext(out var otherEnt))
        {
            // Don't match yourself.
            if (otherEnt == uid)
                continue;

            // Is another storage entity is already anchored here?
            if (HasComp<RandomGasDepositComponent>(otherEnt))
            {
                component.DepositEntity = otherEnt;
                return;
            }
        }

        _popup.PopupEntity(Loc.GetString("gas-deposit-drill-no-resources"), uid);
        args.Cancel();
    }

    public void OnAnchorChanged(EntityUid uid, GasDepositExtractorComponent component, AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            component.DepositEntity = null;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<GasDepositExtractorComponent, TransformComponent>();

        while (query.MoveNext(out var ent, out var drill, out var xform))
        {
        }
    }

    public void OnDepositInit(EntityUid uid, RandomGasDepositComponent component, ComponentInit args)
    {
        if (!_prototype.TryIndex(component.DepositPrototype, out var depositPrototype))
        {
            if (!_prototype.TryGetRandom<GasDepositPrototype>(_random, out var randomPrototype))
                return;
            depositPrototype = (GasDepositPrototype)randomPrototype;
        }
        for (int i = 0; i < (depositPrototype?.Gases?.Length ?? 0) && i < Atmospherics.TotalNumberOfGases; i++)
        {
            var gasRange = depositPrototype!.Gases[i];
            component.Deposit.SetMoles(i, gasRange[0] + _random.NextFloat() * (gasRange[1] - gasRange[0]));
        }
    }

    private void OnDrillUpdate(EntityUid uid, GasDepositExtractorComponent drill, ref AtmosDeviceUpdateEvent args)
    {
        if (!drill.Enabled
            || TryComp<ApcPowerReceiverComponent>(uid, out var power) && !power.Powered
            || !_nodeContainer.TryGetNode(uid, drill.PortName, out PipeNode? port))
        {
            _ambientSound.SetAmbience(uid, false);
            return;
        }

        // How many moles could we theoretically spawn. Cap by pressure and amount.
        var allowableMoles = Math.Max(
            (drill.MaxOutputPressure - port.Air.Pressure) * port.Air.Volume / (drill.SpawnTemperature * Atmospherics.R),
            0);

        if (allowableMoles < Atmospherics.GasMinMoles)
        {
            _ambientSound.SetAmbience(uid, false);
            return;
        }

        var toSpawnReal = Math.Clamp(allowableMoles, , toSpawnTarget);

        _ambientSoundSystem.SetAmbience(uid, toSpawnReal > 0f);
    }
}
