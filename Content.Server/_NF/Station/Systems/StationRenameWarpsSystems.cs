using System.Linq;
using Content.Server.Station.Components;
using Content.Server.Station.Events;
using Content.Server.Station.Systems;
using Content.Server.Warps;

namespace Content.Server._NF.Station.Systems;

public sealed class StationRenameWarpsSystems : EntitySystem
{
    [Dependency] private readonly StationSystem _stationSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StationRenameWarpsComponent, StationRenamedEvent>(OnRenamed);
        SubscribeLocalEvent<StationRenameWarpsComponent, StationPostInitEvent>(OnPostInit);
    }

    private void OnPostInit(EntityUid uid, StationRenameWarpsComponent component, ref StationPostInitEvent args)
    {
        SyncWarpPointsToStation(uid);
    }

    private void OnRenamed(EntityUid uid, StationRenameWarpsComponent component, StationRenamedEvent args)
    {
        SyncWarpPointsToStation(uid);
    }

    public List<Entity<WarpPointComponent>> SyncWarpPointsToStation(EntityUid stationUid, bool? forceAdminOnly = null)
    {
        List<Entity<WarpPointComponent>> ret = new();
        // update all warp points that belong to this station grid
        var query = AllEntityQuery<WarpPointComponent>();
        while (query.MoveNext(out var uid, out var warp))
        {
            var warpStationUid = _stationSystem.GetOwningStation(uid) ?? EntityUid.Invalid;
            if (!warpStationUid.Valid || warpStationUid != stationUid)
                continue;

            if (forceAdminOnly != null)
                warp.AdminOnly = forceAdminOnly.Value;

            if (!warp.UseStationName)
                continue;

            var stationName = Name(warpStationUid);
            warp.Location = stationName;
            ret.Add((uid, warp));
        }
        return ret;
    }

    public List<Entity<WarpPointComponent>> SyncWarpPointsToStations(IEnumerable<EntityUid> stationUids, bool? forceAdminOnly = null)
    {
        List<Entity<WarpPointComponent>> ret = new();
        // update all warp points that belong to this station grid
        var query = AllEntityQuery<WarpPointComponent>();
        while (query.MoveNext(out var uid, out var warp))
        {
            var warpStationUid = _stationSystem.GetOwningStation(uid) ?? EntityUid.Invalid;
            if (!warpStationUid.Valid || !stationUids.Contains(warpStationUid))
                continue;

            if (forceAdminOnly != null)
                warp.AdminOnly = forceAdminOnly.Value;

            if (!warp.UseStationName)
                continue;

            var stationName = Name(warpStationUid);
            warp.Location = stationName;
            ret.Add((uid, warp));
        }
        return ret;
    }

    // Grid name functions
    public List<Entity<WarpPointComponent>> SyncWarpPointsToGrid(EntityUid gridUid, bool? forceAdminOnly = null)
    {
        List<Entity<WarpPointComponent>> ret = new();
        // update all warp points that belong to this station grid
        var query = AllEntityQuery<WarpPointComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var warp, out var xform))
        {
            var warpGridUid = xform.GridUid ?? EntityUid.Invalid;

            if (!warpGridUid.Valid || gridUid != warpGridUid)
                continue;

            if (forceAdminOnly != null)
                warp.AdminOnly = forceAdminOnly.Value;

            if (!warp.UseStationName)
                continue;

            var gridName = Name(warpGridUid);
            warp.Location = gridName;
            ret.Add((uid, warp));
        }
        return ret;
    }

    public List<Entity<WarpPointComponent>> SyncWarpPointsToGrids(IEnumerable<EntityUid> gridUids, bool? forceAdminOnly = null)
    {
        List<Entity<WarpPointComponent>> ret = new();
        // update all warp points that belong to this station grid
        var query = AllEntityQuery<WarpPointComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var warp, out var xform))
        {
            var warpGridUid = xform.GridUid ?? EntityUid.Invalid;

            if (!warpGridUid.Valid || !gridUids.Contains(warpGridUid))
                continue;

            if (forceAdminOnly != null)
                warp.AdminOnly = forceAdminOnly.Value;

            if (!warp.UseStationName)
                continue;

            var gridName = Name(warpGridUid);
            warp.Location = gridName;
            ret.Add((uid, warp));
        }
        return ret;
    }
}
