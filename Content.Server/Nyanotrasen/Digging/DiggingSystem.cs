using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Nyanotrasen.Digging;
using Content.Shared.Physics;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Digging;

public sealed class DiggingSystem : EntitySystem
{
    [Dependency] private readonly TileSystem _tiles = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;
    [Dependency] private readonly SharedToolSystem _tools = default!;
    [Dependency] private readonly TurfSystem _turfs = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EarthDiggingComponent, AfterInteractEvent>(OnDiggingAfterInteract);
        SubscribeLocalEvent<EarthDiggingComponent, EarthDiggingDoAfterEvent>(OnEarthDigComplete);
    }


    private void OnEarthDigComplete(EntityUid shovel, EarthDiggingComponent comp, EarthDiggingDoAfterEvent args)
    {
        var coordinates = GetCoordinates(args.Coordinates);
        if (!TryComp<EarthDiggingComponent>(shovel, out var _))
            return;

        var gridUid = coordinates.GetGridUid(EntityManager);
        if (gridUid == null)
            return;

        var grid = Comp<MapGridComponent>(gridUid.Value);
        var tile = _maps.GetTileRef(gridUid.Value, grid, coordinates);

        if (_tileDefManager[tile.Tile.TypeId] is not ContentTileDefinition tileDef
            || !tileDef.CanShovel
            || string.IsNullOrEmpty(tileDef.BaseTurf)
            || _turfs.IsTileBlocked(tile, CollisionGroup.MobMask))
        {
            return;
        }

        _tiles.DigTile(tile);
    }

    private void OnDiggingAfterInteract(EntityUid uid, EarthDiggingComponent component,
        AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target != null)
            return;

        if (TryDig(args.User, uid, component, args.ClickLocation))
            args.Handled = true;
    }

    private bool TryDig(EntityUid user, EntityUid shovel, EarthDiggingComponent component,
        EntityCoordinates clickLocation)
    {
        ToolComponent? tool = null;
        if (component.ToolComponentNeeded && !TryComp(shovel, out  tool))
            return false;

        var mapUid = clickLocation.GetGridUid(EntityManager);
        if (mapUid == null || !TryComp(mapUid, out MapGridComponent? mapGrid))
            return false;

        var tile = _maps.GetTileRef(mapUid.Value, mapGrid, clickLocation);

        var coordinates = _maps.GridTileToLocal(mapUid.Value, mapGrid, tile.GridIndices);

        if (!_interactionSystem.InRangeUnobstructed(user, coordinates, popup: false))
            return false;

        if (_tileDefManager[tile.Tile.TypeId] is not ContentTileDefinition tileDef
            || !tileDef.CanShovel
            || string.IsNullOrEmpty(tileDef.BaseTurf)
            || _tileDefManager[tileDef.BaseTurf] is not ContentTileDefinition
            || _turfs.IsTileBlocked(tile, CollisionGroup.MobMask))
        {
            return false;
        }

        var ev = new EarthDiggingDoAfterEvent(GetNetCoordinates(clickLocation));
        return _tools.UseTool(
            shovel,
            user,
            target: shovel,
            doAfterDelay: component.Delay,
            toolQualitiesNeeded: new[] { component.QualityNeeded },
            doAfterEv: ev,
            toolComponent: tool
        );
    }
}
