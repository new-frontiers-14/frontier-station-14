using Content.Server.Shuttles.Systems;
using Content.Server.Shuttles.Components;
using Content.Server.Station.Components;
using Content.Server.Cargo.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Shipyard.Components;
using Content.Shared.Shipyard;
using Content.Shared.GameTicking;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Map;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Shared.Coordinates;
using Content.Shared.Shipyard.Events;
using Content.Shared.Mobs.Components;
using Robust.Shared.Containers;
using Content.Server._NF.Smuggling.Components;
using Robust.Shared.Map.Components;

namespace Content.Server.Shipyard.Systems;

public sealed partial class ShipyardSystem : SharedShipyardSystem
{
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly CargoSystem _cargo = default!;
    [Dependency] private readonly DockingSystem _docking = default!;
    [Dependency] private readonly PricingSystem _pricing = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public MapId? ShipyardMap { get; private set; }
    private float _shuttleIndex;
    private const float ShuttleSpawnBuffer = 1f;
    private ISawmill _sawmill = default!;
    private bool _enabled;

    // The type of error from the attempted sale of a ship.
    public enum ShipyardSaleError
    {
        Success, // Ship can be sold.
        Undocked, // Ship is not docked with the station.
        OrganicsAboard, // Sapient intelligence is aboard, cannot sell, would delete the organics
        InvalidShip, // Ship is invalid
        MessageOverwritten, // Overwritten message.
    }

    // TODO: swap to strictly being a formatted message.
    public struct ShipyardSaleResult
    {
        public ShipyardSaleError Error; // Whether or not the ship can be sold.
        public string? OrganicName; // In case an organic is aboard, this will be set to the first that's aboard.
        public string? OverwrittenMessage; // The message to write if Error is MessageOverwritten.
    }

    public override void Initialize()
    {
        base.Initialize();
        _enabled = _configManager.GetCVar(CCVars.Shipyard);
        _configManager.OnValueChanged(CCVars.Shipyard, SetShipyardEnabled, true);
        _sawmill = Logger.GetSawmill("shipyard");

        SubscribeLocalEvent<ShipyardConsoleComponent, ComponentStartup>(OnShipyardStartup);
        SubscribeLocalEvent<ShipyardConsoleComponent, BoundUIOpenedEvent>(OnConsoleUIOpened);
        SubscribeLocalEvent<ShipyardConsoleComponent, ShipyardConsoleSellMessage>(OnSellMessage);
        SubscribeLocalEvent<ShipyardConsoleComponent, ShipyardConsolePurchaseMessage>(OnPurchaseMessage);
        SubscribeLocalEvent<ShipyardConsoleComponent, EntInsertedIntoContainerMessage>(OnItemSlotChanged);
        SubscribeLocalEvent<ShipyardConsoleComponent, EntRemovedFromContainerMessage>(OnItemSlotChanged);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<StationDeedSpawnerComponent, MapInitEvent>(OnInitDeedSpawner);
    }
    public override void Shutdown()
    {
        _configManager.UnsubValueChanged(CCVars.Shipyard, SetShipyardEnabled);
    }
    private void OnShipyardStartup(EntityUid uid, ShipyardConsoleComponent component, ComponentStartup args)
    {
        if (!_enabled)
            return;
        InitializeConsole();
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        CleanupShipyard();
    }

    private void SetShipyardEnabled(bool value)
    {
        if (_enabled == value)
            return;

        _enabled = value;

        if (value)
        {
            SetupShipyardIfNeeded();
        }
        else
        {
            CleanupShipyard();
        }
    }

    /// <summary>
    /// Adds a ship to the shipyard, calculates its price, and attempts to ftl-dock it to the given station
    /// </summary>
    /// <param name="stationUid">The ID of the station to dock the shuttle to</param>
    /// <param name="shuttlePath">The path to the shuttle file to load. Must be a grid file!</param>
    /// <param name="shuttleEntityUid">The EntityUid of the shuttle that was purchased</param>
    public bool TryPurchaseShuttle(EntityUid stationUid, string shuttlePath, [NotNullWhen(true)] out EntityUid? shuttleEntityUid)
    {
        if (!TryComp<StationDataComponent>(stationUid, out var stationData) || !TryAddShuttle(shuttlePath, out var shuttleGrid) || !TryComp<ShuttleComponent>(shuttleGrid, out var shuttleComponent))
        {
            shuttleEntityUid = null;
            return false;
        }

        var price = _pricing.AppraiseGrid((EntityUid) shuttleGrid, null);
        var targetGrid = _station.GetLargestGrid(stationData);


        if (targetGrid == null) //how are we even here with no station grid
        {
            _mapManager.DeleteGrid((EntityUid) shuttleGrid);
            shuttleEntityUid = null;
            return false;
        }

        _sawmill.Info($"Shuttle {shuttlePath} was purchased at {ToPrettyString(stationUid)} for {price:f2}");
        //can do TryFTLDock later instead if we need to keep the shipyard map paused
        _shuttle.TryFTLDock(shuttleGrid.Value, shuttleComponent, targetGrid.Value);
        shuttleEntityUid = shuttleGrid;
        return true;
    }

    /// <summary>
    /// Loads a shuttle into the ShipyardMap from a file path
    /// </summary>
    /// <param name="shuttlePath">The path to the grid file to load. Must be a grid file!</param>
    /// <returns>Returns the EntityUid of the shuttle</returns>
    private bool TryAddShuttle(string shuttlePath, [NotNullWhen(true)] out EntityUid? shuttleGrid)
    {
        shuttleGrid = null;
        SetupShipyardIfNeeded();
        if (ShipyardMap == null)
            return false;

        var loadOptions = new MapLoadOptions()
        {
            Offset = new Vector2(500f + _shuttleIndex, 1f)
        };

        if (!_map.TryLoad(ShipyardMap.Value, shuttlePath, out var gridList, loadOptions) ||
            !EntityManager.TryGetComponent<MapGridComponent>(gridList[0], out var grid))
        {
            _sawmill.Error($"Unable to spawn shuttle {shuttlePath}");
            return false;
        };

        _shuttleIndex += grid.LocalAABB.Width + ShuttleSpawnBuffer;

        //only dealing with 1 grid at a time for now, until more is known about multi-grid drifting
        if (gridList.Count != 1)
        {
            if (gridList.Count < 1)
            {
                _sawmill.Error($"Unable to spawn shuttle {shuttlePath}, no grid found in file");
            };

            if (gridList.Count > 1)
            {
                _sawmill.Error($"Unable to spawn shuttle {shuttlePath}, too many grids present in file");

                foreach (var gridElem in gridList)
                {
                    _mapManager.DeleteGrid(gridElem);
                };
            };

            return false;
        };

        shuttleGrid = gridList[0];
        return true;
    }

    /// <summary>
    /// Checks a shuttle to make sure that it is docked to the given station, and that there are no lifeforms aboard. Then it appraises the grid, outputs to the server log, and deletes the grid
    /// </summary>
    /// <param name="stationUid">The ID of the station that the shuttle is docked to</param>
    /// <param name="shuttleUid">The grid ID of the shuttle to be appraised and sold</param>
    public ShipyardSaleResult TrySellShuttle(EntityUid stationUid, EntityUid shuttleUid, out int bill)
    {
        ShipyardSaleResult result = new ShipyardSaleResult();
        bill = 0;

        if (!TryComp<StationDataComponent>(stationUid, out var stationGrid) || !HasComp<ShuttleComponent>(shuttleUid) || !TryComp<TransformComponent>(shuttleUid, out var xform) || ShipyardMap == null)
        {
            result.Error = ShipyardSaleError.InvalidShip;
            return result;
        }

        var targetGrid = _station.GetLargestGrid(stationGrid);

        if (targetGrid == null)
        {
            result.Error = ShipyardSaleError.InvalidShip;
            return result;
        }

        var gridDocks = _docking.GetDocks((EntityUid) targetGrid);
        var shuttleDocks = _docking.GetDocks(shuttleUid);
        var isDocked = false;

        foreach (var shuttleDock in shuttleDocks)
        {
            foreach (var gridDock in gridDocks)
            {
                if (shuttleDock.Comp.DockedWith == gridDock.Owner)
                {
                    isDocked = true;
                    break;
                }
            }
            if (isDocked)
                break;
        }

        if (!isDocked)
        {
            _sawmill.Warning($"shuttle is not docked to that station");
            result.Error = ShipyardSaleError.Undocked;
            return result;
        }

        var mobQuery = GetEntityQuery<MobStateComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();

        var charName = FoundOrganics(shuttleUid, mobQuery, xformQuery);
        if (charName is not null)
        {
            _sawmill.Warning($"organics on board");
            result.Error = ShipyardSaleError.OrganicsAboard;
            result.OrganicName = charName;
            return result;
        }

        //just yeet and delete for now. Might want to split it into another function later to send back to the shipyard map first to pause for something
        //also superman 3 moment
        if (_station.GetOwningStation(shuttleUid) is { Valid : true } shuttleStationUid)
        {
            _station.DeleteStation(shuttleStationUid);
        }

        bill = (int) _pricing.AppraiseGrid(shuttleUid);
        _mapManager.DeleteGrid(shuttleUid);
        _sawmill.Info($"Sold shuttle {shuttleUid} for {bill}");
        result.Error = ShipyardSaleError.Success;
        return result;
    }

    private void CleanupShipyard()
    {
        if (ShipyardMap == null || !_mapManager.MapExists(ShipyardMap.Value))
        {
            ShipyardMap = null;
            return;
        }

        _mapManager.DeleteMap(ShipyardMap.Value);
    }

    private void SetupShipyardIfNeeded()
    {
        if (ShipyardMap != null && _mapManager.MapExists(ShipyardMap.Value))
            return;

        ShipyardMap = _mapManager.CreateMap();

        _mapManager.SetMapPaused(ShipyardMap.Value, false);
    }

    // <summary>
    // Tries to rename a shuttle deed and update the respective components.
    // Returns true if successful.
    //
    // Null name parts are promptly ignored.
    // </summary>
    public bool TryRenameShuttle(EntityUid uid, ShuttleDeedComponent? shuttleDeed,  string? newName, string? newSuffix)
    {
        if (!Resolve(uid, ref shuttleDeed))
            return false;

        var shuttle = shuttleDeed.ShuttleUid;
        if (shuttle != null
             && _station.GetOwningStation(shuttle.Value) is { Valid : true } shuttleStation)
        {
            shuttleDeed.ShuttleName = newName;
            shuttleDeed.ShuttleNameSuffix = newSuffix;
            Dirty(uid, shuttleDeed);

            var fullName = GetFullName(shuttleDeed);
            _station.RenameStation(shuttleStation, fullName, loud: false);
            _metaData.SetEntityName(shuttle.Value, fullName);
            _metaData.SetEntityName(shuttleStation, fullName);
        }
        else
        {
            _sawmill.Error($"Could not rename shuttle {ToPrettyString(shuttle):entity} to {newName}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Returns the full name of the shuttle component in the form of [prefix] [name] [suffix].
    /// </summary>
    public static string GetFullName(ShuttleDeedComponent comp)
    {
        string?[] parts = { comp.ShuttleName, comp.ShuttleNameSuffix };
        return string.Join(' ', parts.Where(it => it != null));
    }
}
