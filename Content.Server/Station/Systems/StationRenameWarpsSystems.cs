using Content.Server.Station.Components;
using Content.Server.Station.Events;
using Content.Server.Warps;

namespace Content.Server.Station.Systems;

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
        SyncWarpPoints(uid);
    }

    private void OnRenamed(EntityUid uid, StationRenameWarpsComponent component, StationRenamedEvent args)
    {
        SyncWarpPoints(uid);
    }

    private void SyncWarpPoints(EntityUid stationUid)
    {
        // update all warp points that belong to this station grid
        var query = EntityQueryEnumerator<WarpPointComponent>();
        while (query.MoveNext(out var uid, out var warp))
        {
            if (!warp.UseStationName)
                continue;

            var warpStationUid = _stationSystem.GetOwningStation(uid);
            if (warpStationUid != stationUid)
                continue;

            var stationName = Name(warpStationUid.Value);
            warp.Location = stationName;
        }
    }
}
