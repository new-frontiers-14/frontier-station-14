using System.Numerics;
using Content.Server._NF.Atmos.Components;
using Content.Server.Administration.Logs;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Audio;
using Content.Server.DeviceLinking.Systems;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Stack;
using Content.Shared._NF.Atmos.BUI;
using Content.Shared._NF.Atmos.Events;
using Content.Shared._NF.Atmos.Visuals;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.Bank.Components;
using Content.Shared.Construction.Components;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Power;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._NF.Atmos.EntitySystems;

// System for handling gas deposits and machines for extracting from gas deposits
public sealed class GasDepositSystem : EntitySystem
{
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly AmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly StackSystem _stack = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    // The fraction that a deposit's volume should be depleted to before it is considered "low volume".
    private const float LowMoleCoefficient = 0.25f;
    // The maximum distance to check for nearby gas sale points when selling gas.
    private const double DefaultMaxSalePointDistance = 8.0;

    private static readonly SoundPathSpecifier ApproveSound = new("/Audio/Effects/Cargo/ping.ogg");


    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomGasDepositComponent, MapInitEvent>(OnDepositMapInit);

        SubscribeLocalEvent<GasDepositExtractorComponent, MapInitEvent>(OnExtractorMapInit);
        SubscribeLocalEvent<GasDepositExtractorComponent, BoundUIOpenedEvent>(OnExtractorUiOpened);
        SubscribeLocalEvent<GasDepositExtractorComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<GasDepositExtractorComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<GasDepositExtractorComponent, AnchorAttemptEvent>(OnAnchorAttempt);
        SubscribeLocalEvent<GasDepositExtractorComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<GasDepositExtractorComponent, AtmosDeviceUpdateEvent>(OnExtractorUpdate);
        SubscribeLocalEvent<GasDepositExtractorComponent, ActivateInWorldEvent>(OnPumpActivate);

        SubscribeLocalEvent<GasDepositExtractorComponent, GasPressurePumpChangeOutputPressureMessage>(OnOutputPressureChangeMessage);
        SubscribeLocalEvent<GasDepositExtractorComponent, GasPressurePumpToggleStatusMessage>(OnToggleStatusMessage);

        SubscribeLocalEvent<GasSalePointComponent, AtmosDeviceUpdateEvent>(OnSalePointUpdate);

        SubscribeLocalEvent<GasSaleConsoleComponent, BoundUIOpenedEvent>(OnConsoleUiOpened);
        SubscribeLocalEvent<GasSaleConsoleComponent, GasSaleSellMessage>(OnConsoleSell);
        SubscribeLocalEvent<GasSaleConsoleComponent, GasSaleRefreshMessage>(OnConsoleRefresh);
    }

    private void OnExtractorMapInit(Entity<GasDepositExtractorComponent> extractor, ref MapInitEvent args)
    {
        UpdateAppearance(extractor);
    }

    private void OnExtractorUiOpened(Entity<GasDepositExtractorComponent> extractor, ref BoundUIOpenedEvent args)
    {
        Dirty(extractor);
    }

    private void OnPowerChanged(Entity<GasDepositExtractorComponent> extractor, ref PowerChangedEvent args)
    {
        UpdateAppearance(extractor);
    }

    private void OnExamined(Entity<GasDepositExtractorComponent> extractor, ref ExaminedEvent args)
    {
        if (!Transform(extractor).Anchored || !args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("gas-deposit-drill-system-examined",
                ("statusColor", "lightblue"),
                ("pressure", extractor.Comp.TargetPressure)));
        if (TryComp(extractor.Comp.DepositEntity, out RandomGasDepositComponent? deposit))
        {
            args.PushMarkup(Loc.GetString("gas-deposit-drill-system-examined-amount",
                    ("statusColor", "lightblue"),
                    ("value", deposit.Deposit.TotalMoles)));
        }
    }

    public void OnAnchorAttempt(Entity<GasDepositExtractorComponent> extractor, ref AnchorAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp(extractor, out TransformComponent? xform)
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
            if (otherEnt == extractor)
                continue;

            // Is another storage entity is already anchored here?
            if (HasComp<RandomGasDepositComponent>(otherEnt))
            {
                extractor.Comp.DepositEntity = otherEnt.Value;
                return;
            }
        }

        _popup.PopupEntity(Loc.GetString("gas-deposit-drill-no-resources"), extractor);
        args.Cancel();
    }

    public void OnAnchorChanged(Entity<GasDepositExtractorComponent> extractor, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            extractor.Comp.DepositEntity = null;
    }

    public void OnDepositMapInit(Entity<RandomGasDepositComponent> deposit, ref MapInitEvent args)
    {
        if (!_prototype.TryIndex(deposit.Comp.DepositPrototype, out var depositPrototype))
        {
            if (!_prototype.TryGetRandom<GasDepositPrototype>(_random, out var randomPrototype))
                return;
            depositPrototype = (GasDepositPrototype)randomPrototype;
        }
        for (int i = 0; i < (depositPrototype?.Gases?.Length ?? 0) && i < Atmospherics.TotalNumberOfGases; i++)
        {
            var gasRange = depositPrototype!.Gases[i];
            deposit.Comp.Deposit.SetMoles(i, gasRange[0] + _random.NextFloat() * (gasRange[1] - gasRange[0]));
        }
        deposit.Comp.LowMoles = deposit.Comp.Deposit.TotalMoles * LowMoleCoefficient;
    }

    private void OnExtractorUpdate(Entity<GasDepositExtractorComponent> extractor, ref AtmosDeviceUpdateEvent args)
    {
        if (!extractor.Comp.Enabled
            || !TryComp(extractor.Comp.DepositEntity, out RandomGasDepositComponent? depositComp)
            || TryComp<ApcPowerReceiverComponent>(extractor, out var power) && !power.Powered
            || !_nodeContainer.TryGetNode(extractor.Owner, extractor.Comp.PortName, out PipeNode? port)
            || port.NodeGroup is not PipeNet { NodeCount: > 1 } net)
        {
            _ambientSound.SetAmbience(extractor, false);
            SetDepositState(extractor, GasDepositExtractorState.Off);
            return;
        }

        if (depositComp.Deposit.TotalMoles < Atmospherics.GasMinMoles)
        {
            _ambientSound.SetAmbience(extractor, false);
            SetDepositState(extractor, GasDepositExtractorState.Empty);
            return;
        }

        var targetPressure = float.Clamp(extractor.Comp.TargetPressure, 0, extractor.Comp.MaxTargetPressure);

        // How many moles could we theoretically spawn. Cap by pressure, amount, and extractor limit.
        var allowableMoles = (targetPressure - net.Air.Pressure) * net.Air.Volume / (extractor.Comp.OutputTemperature * Atmospherics.R);
        allowableMoles = float.Min(allowableMoles, extractor.Comp.ExtractionRate * args.dt);

        if (allowableMoles < Atmospherics.GasMinMoles)
        {
            _ambientSound.SetAmbience(extractor, false);
            SetDepositState(extractor, GasDepositExtractorState.Blocked);
            return;
        }

        var removed = depositComp.Deposit.Remove(allowableMoles);
        removed.Temperature = extractor.Comp.OutputTemperature;
        _atmosphere.Merge(net.Air, removed);

        _ambientSound.SetAmbience(extractor, true);
        if (depositComp.Deposit.TotalMoles <= depositComp.LowMoles)
            SetDepositState(extractor, GasDepositExtractorState.Low);
        else
            SetDepositState(extractor, GasDepositExtractorState.On);
    }

    private void OnToggleStatusMessage(Entity<GasDepositExtractorComponent> extractor, ref GasPressurePumpToggleStatusMessage args)
    {
        extractor.Comp.Enabled = args.Enabled;
        _adminLog.Add(LogType.AtmosPowerChanged, LogImpact.Low,
            $"{ToPrettyString(args.Actor):player} set the power on {ToPrettyString(extractor):device} to {args.Enabled}");
        Dirty(extractor);
    }

    private void OnPumpActivate(Entity<GasDepositExtractorComponent> extractor, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        if (!TryComp(args.User, out ActorComponent? actor))
            return;

        if (Transform(extractor).Anchored)
        {
            _ui.OpenUi(extractor.Owner, GasPressurePumpUiKey.Key, actor.PlayerSession);
            Dirty(extractor);
        }
        else
        {
            _popup.PopupCursor(Loc.GetString("ui-needs-anchor"), args.User);
        }

        args.Handled = true;
    }

    private void OnOutputPressureChangeMessage(Entity<GasDepositExtractorComponent> extractor, ref GasPressurePumpChangeOutputPressureMessage args)
    {
        extractor.Comp.TargetPressure = Math.Clamp(args.Pressure, 0f, Atmospherics.MaxOutputPressure);
        _adminLog.Add(LogType.AtmosPressureChanged, LogImpact.Low,
            $"{ToPrettyString(args.Actor):player} set the pressure on {ToPrettyString(extractor):device} to {args.Pressure}kPa");
        Dirty(extractor);
    }

    private void SetDepositState(Entity<GasDepositExtractorComponent> extractor, GasDepositExtractorState newState)
    {
        if (newState != extractor.Comp.LastState)
        {
            extractor.Comp.LastState = newState;
            UpdateAppearance(extractor);
        }
    }

    private void UpdateAppearance(Entity<GasDepositExtractorComponent> extractor, AppearanceComponent? appearance = null)
    {
        if (!Resolve(extractor, ref appearance, false))
            return;

        bool pumpOn = extractor.Comp.Enabled && (!TryComp<ApcPowerReceiverComponent>(extractor, out var power) || power.Powered);
        if (!pumpOn)
            _appearance.SetData(extractor, GasDepositExtractorVisuals.State, GasDepositExtractorState.Off, appearance);
        else
            _appearance.SetData(extractor, GasDepositExtractorVisuals.State, extractor.Comp.LastState, appearance);
    }

    // Atmos update: take any gas from the connecting network and push it into the pump.
    private void OnSalePointUpdate(Entity<GasSalePointComponent> salePoint, ref AtmosDeviceUpdateEvent args)
    {
        if (TryComp<ApcPowerReceiverComponent>(salePoint, out var power) && !power.Powered
            || !_nodeContainer.TryGetNode(salePoint.Owner, salePoint.Comp.InletPipePortName, out PipeNode? port)
            || port.NodeGroup is not PipeNet { NodeCount: > 1 } net)
        {
            return;
        }

        if (net.Air.TotalMoles > 0)
        {
            _atmosphere.Merge(salePoint.Comp.GasStorage, net.Air);
            net.Air.Clear();
        }
    }

    private void OnConsoleUiOpened(Entity<GasSaleConsoleComponent> saleConsole, ref BoundUIOpenedEvent args)
    {
        UpdateConsoleInterface(saleConsole);
    }

    private void OnConsoleRefresh(Entity<GasSaleConsoleComponent> saleConsole, ref GasSaleRefreshMessage args)
    {
        UpdateConsoleInterface(saleConsole);
    }

    private void OnConsoleSell(Entity<GasSaleConsoleComponent> saleConsole, ref GasSaleSellMessage args)
    {
        var xform = Transform(saleConsole);
        if (xform.GridUid is not EntityUid gridUid)
        {
            _ui.SetUiState(saleConsole.Owner, GasSaleConsoleUiKey.Key,
            new GasSaleConsoleBoundUserInterfaceState(0, new GasMixture(), false));
            return;
        }

        var mixture = new GasMixture();
        foreach (var salePoint in GetNearbySalePoints(saleConsole, gridUid))
        {
            _atmosphere.Merge(mixture, salePoint.Comp.GasStorage);
            salePoint.Comp.GasStorage.Clear();
        }

        var amount = _atmosphere.GetPrice(mixture);
        if (TryComp<MarketModifierComponent>(saleConsole, out var priceMod))
            amount *= priceMod.Mod;

        var stackPrototype = _prototype.Index(saleConsole.Comp.CashType);
        _stack.Spawn((int)amount, stackPrototype, xform.Coordinates);
        _audio.PlayPvs(ApproveSound, saleConsole);
        _ui.SetUiState(saleConsole.Owner, GasSaleConsoleUiKey.Key,
            new GasSaleConsoleBoundUserInterfaceState(0, new GasMixture(), false));
    }

    private void UpdateConsoleInterface(Entity<GasSaleConsoleComponent> saleConsole)
    {
        if (Transform(saleConsole).GridUid is not EntityUid gridUid)
        {
            _ui.SetUiState(saleConsole.Owner, GasSaleConsoleUiKey.Key,
            new GasSaleConsoleBoundUserInterfaceState(0, new GasMixture(), false));
            return;
        }

        GetNearbyMixtures(saleConsole, gridUid, out var mixture, out var amount);
        if (TryComp<MarketModifierComponent>(saleConsole, out var priceMod))
            amount *= priceMod.Mod;

        _ui.SetUiState(saleConsole.Owner, GasSaleConsoleUiKey.Key,
            new GasSaleConsoleBoundUserInterfaceState((int)amount, mixture, mixture.TotalMoles > 0));
    }

    private void GetNearbyMixtures(EntityUid consoleUid, EntityUid gridUid, out GasMixture mixture, out double value)
    {
        mixture = new GasMixture();

        foreach (var salePoint in GetNearbySalePoints(consoleUid, gridUid))
            _atmosphere.Merge(mixture, salePoint.Comp.GasStorage);

        value = _atmosphere.GetPrice(mixture);
    }

    private List<Entity<GasSalePointComponent>> GetNearbySalePoints(EntityUid consoleUid, EntityUid gridUid)
    {
        List<Entity<GasSalePointComponent>> ret = new();

        var query = AllEntityQuery<GasSalePointComponent, TransformComponent>();

        var consolePosition = Transform(consoleUid).Coordinates.Position;
        var maxSalePointDistance = DefaultMaxSalePointDistance;

        // Get the mapped checking distance from the console
        if (TryComp<GasSaleConsoleComponent>(consoleUid, out var cargoShuttleComponent))
            maxSalePointDistance = cargoShuttleComponent.SellPointDistance;

        while (query.MoveNext(out var uid, out var comp, out var compXform))
        {
            if (compXform.ParentUid != gridUid
                || !compXform.Anchored
                || Vector2.Distance(consolePosition, compXform.Coordinates.Position) > maxSalePointDistance)
            {
                continue;
            }

            ret.Add((uid, comp));
        }

        return ret;
    }
}
