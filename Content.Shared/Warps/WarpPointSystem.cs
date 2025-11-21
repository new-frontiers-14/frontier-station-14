using Content.Shared.Examine;
using Content.Shared.Ghost;
<<<<<<< HEAD:Content.Server/Warps/WarpPointSystem.cs
using Content.Shared.Warps;
using Content.Server.Station.Systems; // Frontier
=======
>>>>>>> 9f36a3b4ea321ca0cb8d0fa0f2a585b14d136d78:Content.Shared/Warps/WarpPointSystem.cs

namespace Content.Shared.Warps;

public sealed class WarpPointSystem : EntitySystem
{
    [Dependency] private readonly StationSystem _station = default!; // Frontier
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WarpPointComponent, ExaminedEvent>(OnWarpPointExamine);
        SubscribeLocalEvent<WarpPointComponent, ComponentStartup>(OnStartup); // Frontier
    }

    private void OnWarpPointExamine(EntityUid uid, WarpPointComponent component, ExaminedEvent args)
    {
        if (!HasComp<GhostComponent>(args.Examiner))
            return;

        var loc = component.Location == null ? "<null>" : $"'{component.Location}'";
        args.PushText(Loc.GetString("warp-point-component-on-examine-success", ("location", loc)));
    }

    // Frontier
    private void OnStartup(EntityUid uid, WarpPointComponent component, ComponentStartup args)
    {
        if (component.QueryStationName
            && _station.GetOwningStation(uid) is { Valid: true } station
            && TryComp(station, out MetaDataComponent? stationMetadata))
        {
            component.Location = stationMetadata.EntityName;
        }
        else if (component.QueryGridName
            && TryComp(uid, out TransformComponent? xform)
            && xform.GridUid is { Valid: true } grid
            && TryComp(grid, out MetaDataComponent? gridMetadata))
        {
            component.Location = gridMetadata.EntityName;
        }
    }
    // End Frontier
}
