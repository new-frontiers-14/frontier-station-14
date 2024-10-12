using System.Linq;
using Content.Shared.GameTicking;

namespace Content.Client._NF.LateJoin;

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

    public static string GetStationNameWithJobCount(this StationJobInformation stationJobInformation)
    {
        var jobCountString = stationJobInformation.GetJobCountString();
        var stationNameWithJobCount = string.IsNullOrEmpty(jobCountString)
            ? stationJobInformation.StationName
            : stationJobInformation.StationName + jobCountString;

        return stationNameWithJobCount;
    }

    /**
     * This method returns various strings that represent the job count of a station.
     * If there are unlimited jobs available, it will return the job count followed by a "+".
     * If there are no jobs available, it will return an empty string.
     */
    public static string GetJobCountString(this StationJobInformation stationJobInformation)
    {
        var jobCount = stationJobInformation.GetJobCount();
        var hasUnlimitedJobs = stationJobInformation.HasUnlimitedJobs();
        return jobCount.WrapJobCountInParentheses(hasUnlimitedJobs);
    }

    /**
     * This method returns various strings that represent the job count of a list of stations.
     * If there are unlimited jobs available, it will return the job count followed by a "+".
     * If there are no jobs available, it will return an empty string.
     */
    public static string GetJobSumCountString(this Dictionary<NetEntity, StationJobInformation> obj)
    {
        var jobCount = obj.Values.Sum(stationJobInformation => stationJobInformation.GetJobCount());
        var hasUnlimitedJobs = obj.Values.Any(stationJobInformation => stationJobInformation.HasUnlimitedJobs());
        return jobCount.WrapJobCountInParentheses(hasUnlimitedJobs);
    }

    /**
     * One source of truth for the logic of whether a station has unlimited positions in one of its jobs.
     * This is used to determine whether to display a "+" after the job count, or not to display the job count.
     */
    private static bool HasUnlimitedJobs(this StationJobInformation stationJobInformation)
    {
        return stationJobInformation.JobsAvailable.Values.Any(count => count == null);
    }

    private static int? GetJobCount(this StationJobInformation stationJobInformation)
    {
        return stationJobInformation.JobsAvailable.Values.Sum();
    }

    public static string WrapJobCountInParentheses(this int? jobCount, bool hasUnlimitedJobs = false)
    {
        if (jobCount is 0 or null)
        {
            return "";
        }

        var jobCountString = jobCount > 0 ? $"{jobCount}" : "";
        if (hasUnlimitedJobs && jobCount > 0)
        {
            jobCountString += "+";
        }
        return $" ({jobCountString})";
    }

}
