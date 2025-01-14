using Content.Server._NF.Atmos.Components;
using Content.Server.Administration.Logs;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Audio;
using Content.Server.DeviceLinking.Events;
using Content.Server.DeviceLinking.Systems;
using Content.Server.DeviceNetwork;
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
using Content.Shared.DeviceNetwork;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Power;
using Robust.Server.GameObjects;
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

    private const float LowMoleCoefficient = 0.25f;

    // Surveillance camera data. This generally should contain nothing
    // except for the subnet that this camera is on -
    // this is because of the fact that the PacketEvent already
    // contains the sender UID, and that this will always be targeted
    // towards the sender that pinged the camera.
    public const string GasSaleQueryCommand = "gas_sale_query";
    public const string GasSaleQueryResponse = "gas_sale_contents";
    public const string GasSaleSellCommand = "gas_sale_sell";
    public const string GasSaleSellResponse = "gas_sale_value";

    public const string GasSaleQueryData = "gas_sale_contents_data";
    public const string GasSaleSellData = "gas_sale_value_data";

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomGasDepositComponent, ComponentInit>(OnDepositInit);

        SubscribeLocalEvent<GasDepositExtractorComponent, ComponentInit>(OnExtractorInit);
        SubscribeLocalEvent<GasDepositExtractorComponent, BoundUIOpenedEvent>(OnExtractorUiOpened);
        SubscribeLocalEvent<GasDepositExtractorComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<GasDepositExtractorComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<GasDepositExtractorComponent, AnchorAttemptEvent>(OnAnchorAttempt);
        SubscribeLocalEvent<GasDepositExtractorComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<GasDepositExtractorComponent, AtmosDeviceUpdateEvent>(OnExtractorUpdate);
        SubscribeLocalEvent<GasDepositExtractorComponent, ActivateInWorldEvent>(OnPumpActivate);

        SubscribeLocalEvent<GasDepositExtractorComponent, GasPressurePumpChangeOutputPressureMessage>(OnOutputPressureChangeMessage);
        SubscribeLocalEvent<GasDepositExtractorComponent, GasPressurePumpToggleStatusMessage>(OnToggleStatusMessage);

        SubscribeLocalEvent<GasSalePointComponent, MapInitEvent>(OnSalePointMapInit);
        SubscribeLocalEvent<GasSalePointComponent, AtmosDeviceUpdateEvent>(OnSalePointUpdate);

        SubscribeLocalEvent<GasSaleConsoleComponent, MapInitEvent>(OnSaleConsoleMapInit);
        SubscribeLocalEvent<GasSaleConsoleComponent, BoundUIOpenedEvent>(OnConsoleUiOpened);
        SubscribeLocalEvent<GasSaleConsoleComponent, GasSaleSellMessage>(OnSaleRequest);
        SubscribeLocalEvent<GasSaleConsoleComponent, SignalReceivedEvent>(OnConsoleSignalReceived);

    }

    private void OnExtractorInit(EntityUid uid, GasDepositExtractorComponent extractor, ComponentInit args)
    {
        UpdateAppearance(uid, extractor);
    }

    private void OnExtractorUiOpened(EntityUid uid, GasDepositExtractorComponent extractor, BoundUIOpenedEvent args)
    {
        DirtyUI(uid, extractor);
    }

    private void OnPowerChanged(EntityUid uid, GasDepositExtractorComponent component, ref PowerChangedEvent args)
    {
        UpdateAppearance(uid, component);
    }

    private void OnExamined(EntityUid uid, GasDepositExtractorComponent extractor, ExaminedEvent args)
    {
        if (!EntityManager.GetComponent<TransformComponent>(uid).Anchored || !args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("gas-deposit-drill-system-examined",
                ("statusColor", "lightblue"),
                ("pressure", extractor.TargetPressure)));
        if (extractor.DepositEntity != null)
        {
            args.PushMarkup(Loc.GetString("gas-deposit-drill-system-examined-amount",
                    ("statusColor", "lightblue"),
                    ("value", extractor.DepositEntity.Value.Comp.Deposit.TotalMoles)));
        }
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
        component.LowMoles = component.Deposit.TotalMoles * LowMoleCoefficient;
    }

    private void OnExtractorUpdate(EntityUid uid, GasDepositExtractorComponent extractor, ref AtmosDeviceUpdateEvent args)
    {
        if (!extractor.Enabled
            || extractor.DepositEntity == null
            || TryComp<ApcPowerReceiverComponent>(uid, out var power) && !power.Powered
            || !_nodeContainer.TryGetNode(uid, extractor.PortName, out PipeNode? port)
            || port.NodeGroup is not PipeNet { NodeCount: > 1 } net)
        {
            _ambientSound.SetAmbience(uid, false);
            SetDepositState(uid, GasDepositExtractorState.Off, extractor);
            return;
        }

        var depositComp = extractor.DepositEntity.Value.Comp;
        if (depositComp.Deposit.TotalMoles < Atmospherics.GasMinMoles)
        {
            _ambientSound.SetAmbience(uid, false);
            SetDepositState(uid, GasDepositExtractorState.Empty, extractor);
            return;
        }

        var targetPressure = float.Clamp(extractor.TargetPressure, 0, extractor.MaxOutputPressure);

        // How many moles could we theoretically spawn. Cap by pressure, amount, and extractor limit.
        var allowableMoles = (targetPressure - net.Air.Pressure) * net.Air.Volume / (extractor.OutputTemperature * Atmospherics.R);
        allowableMoles = float.Min(allowableMoles, extractor.ExtractionRate * args.dt);

        if (allowableMoles < Atmospherics.GasMinMoles)
        {
            _ambientSound.SetAmbience(uid, false);
            SetDepositState(uid, GasDepositExtractorState.Blocked, extractor);
            return;
        }

        var removed = depositComp.Deposit.Remove(allowableMoles);
        removed.Temperature = extractor.OutputTemperature;
        _atmosphere.Merge(net.Air, removed);

        _ambientSound.SetAmbience(uid, true);
        if (depositComp.Deposit.TotalMoles <= depositComp.LowMoles)
            SetDepositState(uid, GasDepositExtractorState.Low, extractor);
        else
            SetDepositState(uid, GasDepositExtractorState.On, extractor);
    }

    private void OnToggleStatusMessage(EntityUid uid, GasDepositExtractorComponent extractor, GasPressurePumpToggleStatusMessage args)
    {
        extractor.Enabled = args.Enabled;
        _adminLog.Add(LogType.AtmosPowerChanged, LogImpact.Low,
            $"{ToPrettyString(args.Actor):player} set the power on {ToPrettyString(uid):device} to {args.Enabled}");
        DirtyUI(uid, extractor);
    }

    private void OnPumpActivate(EntityUid uid, GasDepositExtractorComponent pump, ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        if (!TryComp(args.User, out ActorComponent? actor))
            return;

        if (Transform(uid).Anchored)
        {
            _ui.OpenUi(uid, GasPressurePumpUiKey.Key, actor.PlayerSession);
            DirtyUI(uid, pump);
        }
        else
        {
            _popup.PopupCursor(Loc.GetString("gas-deposit-drill-ui-needs-anchor"), args.User);
        }

        args.Handled = true;
    }

    private void OnOutputPressureChangeMessage(EntityUid uid, GasDepositExtractorComponent extractor, GasPressurePumpChangeOutputPressureMessage args)
    {
        extractor.TargetPressure = Math.Clamp(args.Pressure, 0f, Atmospherics.MaxOutputPressure);
        _adminLog.Add(LogType.AtmosPressureChanged, LogImpact.Low,
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

    private void SetDepositState(EntityUid uid, GasDepositExtractorState newState, GasDepositExtractorComponent? extractor = null)
    {
        if (!Resolve(uid, ref extractor, false))
            return;

        if (newState != extractor.LastState)
        {
            extractor.LastState = newState;
            UpdateAppearance(uid, extractor);
        }
    }

    private void UpdateAppearance(EntityUid uid, GasDepositExtractorComponent? extractor = null, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref extractor, ref appearance, false))
            return;

        bool pumpOn = extractor.Enabled && (!TryComp<ApcPowerReceiverComponent>(uid, out var power) || power.Powered);
        if (!pumpOn)
            _appearance.SetData(uid, GasDepositExtractorVisuals.State, GasDepositExtractorState.Off, appearance);
        else
            _appearance.SetData(uid, GasDepositExtractorVisuals.State, extractor.LastState, appearance);
    }

    private void OnSaleConsoleMapInit(EntityUid uid, GasSaleConsoleComponent component, MapInitEvent args)
    {
        // Console: sends commands, receives responses.
        _deviceLink.EnsureSourcePorts(uid, component.CommandPortName);
        _deviceLink.EnsureSinkPorts(uid, component.ResponsePortName);
    }

    private void OnSalePointMapInit(EntityUid uid, GasSalePointComponent component, MapInitEvent args)
    {
        // Gas point: receives commands, sends responses.
        _deviceLink.EnsureSourcePorts(uid, component.ResponsePortName);
        _deviceLink.EnsureSinkPorts(uid, component.CommandPortName);
    }

    // Atmos update: take any gas from the connecting network and push it into the pump.
    private void OnSalePointUpdate(EntityUid uid, GasSalePointComponent component, ref AtmosDeviceUpdateEvent args)
    {
        if (TryComp<ApcPowerReceiverComponent>(uid, out var power) && !power.Powered
            || !_nodeContainer.TryGetNode(uid, component.InletPipePortName, out PipeNode? port)
            || port.NodeGroup is not PipeNet { NodeCount: > 1 } net)
        {
            return;
        }

        if (net.Air.TotalMoles > 0)
        {
            _atmosphere.Merge(component.GasStorage, net.Air);
            net.Air.Clear();
            SendSalePointContents(uid, component);
        }
    }

    private void OnSaleRequest(EntityUid uid, GasSaleConsoleComponent component, GasSaleSellMessage args)
    {
        if (TryComp<ApcPowerReceiverComponent>(uid, out var power) && !power.Powered)
            return;

        NetworkPayload payload = new NetworkPayload()
        {
            { DeviceNetworkConstants.Command, GasSaleSellCommand }
        };

        _deviceLink.InvokePort(uid, component.CommandPortName, payload);
    }

    private void SendSalePointContents(EntityUid uid, GasSalePointComponent component)
    {
        var payload = new NetworkPayload()
        {
            { DeviceNetworkConstants.Command, GasSaleQueryResponse },
            { GasSaleQueryData, component.GasStorage }
        };
        _deviceLink.InvokePort(uid, component.ResponsePortName, payload);
    }

    private void OnConsoleSignalReceived(EntityUid uid, GasSaleConsoleComponent component, ref SignalReceivedEvent args)
    {
        var payload = args.Data;
        if (payload == null || !payload.ContainsKey(DeviceNetworkConstants.Command))
            return;

        var command = payload[DeviceNetworkConstants.Command];
        switch (command)
        {
            case GasSaleQueryResponse:
                // Add contents to UI
                if (!payload.TryGetValue(GasSaleQueryData, out GasMixture? mixture)
                    || mixture == null)
                    return;
                component.LastKnownMixture = mixture;
                break;
            case GasSaleSellResponse:
                if (!payload.TryGetValue(GasSaleSellData, out int? saleValue)
                    || saleValue == null
                    || saleValue.Value <= 0)
                    return;
                _stack.SpawnMultiple(component.Currency, saleValue.Value, Transform(uid).Coordinates);
                UpdateConsoleInterface(uid, component, new GasMixture());
                component.LastKnownMixture = new();
                break;
        }
    }

    private void OnSalePointSignalReceived(EntityUid uid, GasSalePointComponent component, ref SignalReceivedEvent args)
    {
        var payload = args.Data;
        if (payload == null || !payload.ContainsKey(DeviceNetworkConstants.Command))
            return;

        var command = payload[DeviceNetworkConstants.Command];
        switch (command)
        {
            case GasSaleQueryCommand:
                SendSalePointContents(uid, component);
                break;
            case GasSaleSellCommand:
                var salePayload = new NetworkPayload()
                {
                    { DeviceNetworkConstants.Command, GasSaleSellResponse },
                    { GasSaleSellData, (int)_atmosphere.GetPrice(component.GasStorage) }
                };
                _deviceLink.InvokePort(uid, component.ResponsePortName, salePayload);
                component.GasStorage.Clear();
                break;
        }
    }

    private void OnConsoleUiOpened(EntityUid uid, GasSaleConsoleComponent component, BoundUIOpenedEvent args)
    {
        UpdateConsoleInterface(uid, component, component.LastKnownMixture);
    }

    private void UpdateConsoleInterface(EntityUid uid, GasSaleConsoleComponent component, GasMixture mixture)
    {
        float marketMod = 1.0f;
        if (TryComp<MarketModifierComponent>(uid, out var mod))
            marketMod = mod.Mod;

        _ui.SetUiState(uid, GasSaleConsoleUiKey.Key, new GasSaleConsoleBoundUserInterfaceState((int)(_atmosphere.GetPrice(mixture) * marketMod), mixture, mixture.TotalMoles > 0));
    }
}
