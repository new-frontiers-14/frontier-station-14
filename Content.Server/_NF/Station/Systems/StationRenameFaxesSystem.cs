using Content.Server.Station.Components;
using Content.Server.Station.Events;
using Content.Shared.Fax.Components;

namespace Content.Server.Station.Systems;

public sealed class StationRenameFaxesSystem : EntitySystem
{
    [Dependency] private readonly StationSystem _stationSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StationRenameFaxesComponent, StationRenamedEvent>(OnRenamed);
        SubscribeLocalEvent<StationRenameFaxesComponent, StationPostInitEvent>(OnPostInit);
    }

    private void OnPostInit(EntityUid uid, StationRenameFaxesComponent component, ref StationPostInitEvent args)
    {
        SyncFaxesNames(uid);
    }

    private void OnRenamed(EntityUid uid, StationRenameFaxesComponent component, StationRenamedEvent args)
    {
        SyncFaxesNames(uid);
    }

    private void SyncFaxesNames(EntityUid stationUid)
    {
        // update all faxes that belong to this station grid
        var query = EntityQueryEnumerator<FaxMachineComponent>();
        while (query.MoveNext(out var uid, out var fax))
        {
            if (!fax.UseStationName)
                continue;

            var faxStationUid = _stationSystem.GetOwningStation(uid);
            if (faxStationUid != stationUid)
                continue;

            var stationName = "";

            if (!string.IsNullOrEmpty(fax.StationNamePrefix))
            {
                stationName += fax.StationNamePrefix + " ";
            }

            stationName += Name(faxStationUid.Value);

            if (!string.IsNullOrEmpty(fax.StationNameSuffix))
            {
                stationName += " " + fax.StationNameSuffix;
            }

            fax.FaxName = stationName;
        }
    }
}
