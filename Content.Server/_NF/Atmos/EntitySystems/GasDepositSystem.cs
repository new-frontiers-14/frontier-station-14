using Content.Server._NF.Atmos.Components;
using Content.Server.Administration.Logs;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Audio;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.Construction.Components;
using Content.Shared.Database;
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
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly AmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomGasDepositComponent, ComponentInit>(OnDepositInit);

        SubscribeLocalEvent<GasDepositExtractorComponent, AnchorAttemptEvent>(OnAnchorAttempt);
        SubscribeLocalEvent<GasDepositExtractorComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<GasDepositExtractorComponent, AtmosDeviceUpdateEvent>(OnExtractorUpdate);

        SubscribeLocalEvent<GasDepositExtractorComponent, GasPressurePumpChangeOutputPressureMessage>(OnOutputPressureChangeMessage);
        SubscribeLocalEvent<GasDepositExtractorComponent, GasPressurePumpToggleStatusMessage>(OnToggleStatusMessage);

    }

    public void OnAnchorAttempt(EntityUid uid, GasDepositExtractorComponent component, AnchorAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp(uid, out TransformComponent? xform)
            || xform.GridUid is not { Valid: true } grid
            || !TryComp(grid, out MapGridComponent? gridComp))
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
            if (TryComp<RandomGasDepositComponent>(otherEnt, out var deposit))
            {
                component.DepositEntity = (otherEnt.Value, deposit);
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

    private void OnExtractorUpdate(EntityUid uid, GasDepositExtractorComponent extractor, ref AtmosDeviceUpdateEvent args)
    {
        if (!extractor.Enabled
            || extractor.DepositEntity == null
            || TryComp<ApcPowerReceiverComponent>(uid, out var power) && !power.Powered
            || !_nodeContainer.TryGetNode(uid, extractor.PortName, out PipeNode? port))
        {
            _ambientSound.SetAmbience(uid, false);
            return;
        }

        var targetPressure = float.Clamp(extractor.TargetPressure, 0, extractor.MaxOutputPressure);

        // How many moles could we theoretically spawn. Cap by pressure and amount.
        var allowableMoles = float.Max(0,
            (targetPressure - port.Air.Pressure) * port.Air.Volume / (extractor.OutputTemperature * Atmospherics.R));

        if (allowableMoles < Atmospherics.GasMinMoles)
        {
            _ambientSound.SetAmbience(uid, false);
            return;
        }

        var removed = extractor.DepositEntity.Value.Comp.Deposit.Remove(allowableMoles);
        removed.Temperature = extractor.OutputTemperature;
        _atmosphere.Merge(port.Air, removed);

        _ambientSound.SetAmbience(uid, true);
    }

    private void OnToggleStatusMessage(EntityUid uid, GasDepositExtractorComponent extractor, GasPressurePumpToggleStatusMessage args)
    {
        extractor.Enabled = args.Enabled;
        _adminLog.Add(LogType.AtmosPowerChanged, LogImpact.Low,
            $"{ToPrettyString(args.Actor):player} set the power on {ToPrettyString(uid):device} to {args.Enabled}");
        DirtyUI(uid, extractor);
    }

    private void OnOutputPressureChangeMessage(EntityUid uid, GasDepositExtractorComponent extractor, GasPressurePumpChangeOutputPressureMessage args)
    {
        extractor.TargetPressure = Math.Clamp(args.Pressure, 0f, Atmospherics.MaxOutputPressure);
        _adminLog.Add(LogType.AtmosPressureChanged, LogImpact.Medium,
            $"{ToPrettyString(args.Actor):player} set the pressure on {ToPrettyString(uid):device} to {args.Pressure}kPa");
        DirtyUI(uid, extractor);
    }

    private void DirtyUI(EntityUid uid, GasDepositExtractorComponent? extractor)
    {
        if (!Resolve(uid, ref extractor))
            return;

        _ui.SetUiState(uid, GasPressurePumpUiKey.Key,
            new GasPressurePumpBoundUserInterfaceState(EntityManager.GetComponent<MetaDataComponent>(uid).EntityName, extractor.TargetPressure, extractor.Enabled));
    }
}
