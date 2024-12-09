using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Content.Shared.Power;
using Robust.Server.GameObjects;
using Robust.Shared.Utility;
using Content.Shared.UserInterface;
using Content.Shared._NF.Atmos.BUIStates;
using Content.Server._NF.Atmos.Components;
using Content.Server.Atmos.Piping.Binary.Components;
using Content.Shared._NF.Atmos.Components; // Frontier

namespace Content.Server._NF.Atmos.Systems;

public sealed partial class GaslockConsoleSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private EntityQuery<TransformComponent> _xformQuery;

    private readonly HashSet<Entity<GaslockConsoleComponent>> _consoles = new();

    public override void Initialize()
    {
        base.Initialize();

        _xformQuery = GetEntityQuery<TransformComponent>();

        SubscribeLocalEvent<GaslockConsoleComponent, PowerChangedEvent>(OnConsolePowerChange);
        SubscribeLocalEvent<GaslockConsoleComponent, AnchorStateChangedEvent>(OnConsoleAnchorChange);
        SubscribeLocalEvent<GaslockConsoleComponent, ActivatableUIOpenAttemptEvent>(OnConsoleUIOpenAttempt);
        Subs.BuiEvents<GaslockConsoleComponent>(GaslockConsoleUiKey.Key, subs =>
        {
            subs.Event<BoundUIClosedEvent>(OnConsoleUIClose);
        });

        SubscribeLocalEvent<DockEvent>(OnDock);
        SubscribeLocalEvent<UndockEvent>(OnUndock);
    }
    private void OnDock(DockEvent ev)
    {
        RefreshShuttleConsoles();
    }

    private void OnUndock(UndockEvent ev)
    {
        RefreshShuttleConsoles();
    }

    /// <summary>
    /// Refreshes all the shuttle console data for a particular grid.
    /// </summary>
    public void RefreshShuttleConsoles(EntityUid gridUid)
    {
        _consoles.Clear();
        _lookup.GetChildEntities(gridUid, _consoles);
        GaslockState? dockState = null;

        foreach (var entity in _consoles)
        {
            UpdateState(entity, ref dockState);
        }
    }

    /// <summary>
    /// Refreshes all of the data for shuttle consoles.
    /// </summary>
    public void RefreshShuttleConsoles()
    {
        var query = AllEntityQuery<GaslockConsoleComponent>();
        GaslockState? dockState = null;

        while (query.MoveNext(out var uid, out _))
        {
            UpdateState(uid, ref dockState);
        }
    }

    /// <summary>
    /// Stop piloting if the window is closed.
    /// </summary>
    private void OnConsoleUIClose(EntityUid uid, GaslockConsoleComponent component, BoundUIClosedEvent args)
    {
        if ((GaslockConsoleUiKey)args.UiKey != GaslockConsoleUiKey.Key)
        {
            return;
        }
    }

    private void OnConsoleUIOpenAttempt(EntityUid uid, GaslockConsoleComponent component,
        ActivatableUIOpenAttemptEvent args)
    {
    }

    private void OnConsoleAnchorChange(EntityUid uid, GaslockConsoleComponent component,
        ref AnchorStateChangedEvent args)
    {
        GaslockState? dockState = null;
        UpdateState(uid, ref dockState);
    }

    private void OnConsolePowerChange(EntityUid uid, GaslockConsoleComponent component, ref PowerChangedEvent args)
    {
        GaslockState? dockState = null;
        UpdateState(uid, ref dockState);
    }

    /// <summary>
    /// Returns the position and angle of all dockingcomponents.
    /// </summary>
    public Dictionary<NetEntity, List<GaslockPortState>> GetAllGaslocks()
    {
        // TODO: NEED TO MAKE SURE THIS UPDATES ON ANCHORING CHANGES!
        var result = new Dictionary<NetEntity, List<GaslockPortState>>();
        var query = AllEntityQuery<DockingComponent, DockablePumpComponent, TransformComponent, MetaDataComponent>();

        while (query.MoveNext(out var uid, out var dock, out _, out var xform, out var metadata))
        {
            if (xform.ParentUid != xform.GridUid)
                continue;

            // I don't think we want volume pumps, but if we support other types, this may need to handle them separately.
            if (!TryComp(uid, out GasPressurePumpComponent? pump))
                continue;

            var gridDocks = result.GetOrNew(GetNetEntity(xform.GridUid.Value));

            var state = new GaslockPortState()
            {
                Name = metadata.EntityName,
                Coordinates = GetNetCoordinates(xform.Coordinates),
                Angle = xform.LocalRotation,
                Entity = GetNetEntity(uid),
                GridDockedWith =
                    _xformQuery.TryGetComponent(dock.DockedWith, out var otherDockXform) ?
                    GetNetEntity(otherDockXform.GridUid) :
                    null,
                Pressure = pump.TargetPressure,
                Inwards = pump.PumpingInwards,
                Enabled = pump.Enabled,
                LabelName = dock.Name, // Frontier: docking labels
                RadarColor = dock.RadarColor, // Frontier
                HighlightedRadarColor = dock.HighlightedRadarColor, // Frontier
                DockType = dock.DockType, // Frontier
                ReceiveOnly = dock.ReceiveOnly, // Frontier
            };

            gridDocks.Add(state);
        }

        return result;
    }

    private void UpdateState(EntityUid consoleUid, ref GaslockState? gaslockState)
    {
        gaslockState ??= GetGaslockState();

        if (_ui.HasUi(consoleUid, GaslockConsoleUiKey.Key) && TryComp(consoleUid, out TransformComponent? xform))
        {
            GaslockConsoleBoundUserInterfaceState gaslockBUIState = new(GetNetCoordinates(xform.Coordinates), gaslockState);
            _ui.SetUiState(consoleUid, GaslockConsoleUiKey.Key, gaslockBUIState);
        }
    }

    /// <summary>
    /// Global for all shuttles.
    /// </summary>
    /// <returns></returns>
    public GaslockState GetGaslockState()
    {
        var gaslocks = GetAllGaslocks();
        return new GaslockState(gaslocks);
    }
}
