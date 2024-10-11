using System.Linq;
using Content.Shared.GameTicking;

namespace Content.Client._NF.Latejoin;

public static class StationJobInformationExtensions
{
    public static bool IsAnyStationAvailable(IReadOnlyDictionary<NetEntity, StationJobInformation> obj)
    {
        return obj.Values.Any(station =>
            station is { IsLateJoinStation: true, JobsAvailable.Count: > 0 }
        );
    }

    public static bool IsAnyCrewJobAvailable(IReadOnlyDictionary<NetEntity, StationJobInformation> obj)
    {
        return obj.Values.Any(station =>
            station is { IsLateJoinStation: false, JobsAvailable.Count: > 0 }
        );
    }
}
