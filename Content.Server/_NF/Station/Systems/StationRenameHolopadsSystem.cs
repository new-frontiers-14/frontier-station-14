using Content.Server.Station.Components;
using Content.Server.Station.Events;
using Content.Shared.Fax.Components;
using Content.Shared.Holopad;
using Content.Shared.Labels.Components;

namespace Content.Server.Station.Systems;

public sealed class StationRenameHolopadsSystem : EntitySystem
{
    [Dependency] private readonly StationSystem _stationSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StationRenameHolopadsComponent, StationPostInitEvent>(OnPostInit);
    }

    private void OnPostInit(EntityUid uid, StationRenameHolopadsComponent component, ref StationPostInitEvent args)
    {
        SyncHolopadsNames(uid);
    }

    private void SyncHolopadsNames(EntityUid stationUid)
    {
        // update all holopads that belong to this station grid
        var query = EntityQueryEnumerator<HolopadComponent>();
        while (query.MoveNext(out var uid, out var pad))
        {
            if (!pad.UseStationName)
                continue;

            var padStationUid = _stationSystem.GetOwningStation(uid);
            if (padStationUid != stationUid)
                continue;

            var padName = "";

            if (!string.IsNullOrEmpty(pad.StationNamePrefix))
            {
                padName += pad.StationNamePrefix + " ";
            }

            padName += Name(padStationUid.Value);

            if (!string.IsNullOrEmpty(pad.StationNameSuffix))
            {
                padName += " " + pad.StationNameSuffix;
            }

            var padLabel = EnsureComp<LabelComponent>(uid);
            padLabel.CurrentLabel = padName;
        }
}
}
