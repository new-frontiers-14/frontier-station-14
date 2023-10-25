using Content.Server.Administration.Logs;
using Content.Server.Power.Components;
using Content.Server.Stack;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Stacks;
using Robust.Shared.Map;

namespace Content.Server.Power.EntitySystems;

public sealed partial class CableSystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    private void InitializeCablePlacer()
    {
        SubscribeLocalEvent<CablePlacerComponent, AfterInteractEvent>(OnCablePlacerAfterInteract);
    }

    private void OnCablePlacerAfterInteract(EntityUid uid, CablePlacerComponent component, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach) return;

        if (component.CablePrototypeId == null) return;

        if(!_mapManager.TryGetGrid(args.ClickLocation.GetGridUid(EntityManager), out var grid))
            return;

        var snapPos = grid.TileIndicesFor(args.ClickLocation);
        var tileDef = (ContentTileDefinition) _tileManager[grid.GetTileRef(snapPos).Tile.TypeId];

        if (!tileDef.IsSubFloor || !tileDef.Sturdy)
            return;

        foreach (var anchored in grid.GetAnchoredEntities(snapPos))
        {
            if (TryComp<CableComponent>(anchored, out var wire) && wire.CableType == component.BlockingCableType)
                return;
        }

        if (TryComp<StackComponent>(component.Owner, out var stack) && !_stack.Use(component.Owner, 1, stack))
            return;

        var newCable = EntityManager.SpawnEntity(component.CablePrototypeId, grid.GridTileToLocal(snapPos));
        _adminLogger.Add(LogType.Construction, LogImpact.Low,
            $"{ToPrettyString(args.User):player} placed {ToPrettyString(newCable):cable} at {Transform(newCable).Coordinates}");
        args.Handled = true;
    }
}
