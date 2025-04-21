using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared._NF.Roles.Systems;

/// <summary>
/// This handles job tracking for station jobs that should be reopened on cryo.
/// </summary>
public abstract class SharedJobTrackingSystem : EntitySystem
{
    public static readonly ProtoId<JobPrototype>[] ReopenExceptions = ["Contractor", "Pilot", "Mercenary", "Borg"];

    public static bool JobShouldBeReopened(ProtoId<JobPrototype> job)
    {
        foreach (var reopenJob in ReopenExceptions)
        {
            if (job == reopenJob)
                return false;
        }
        return true;
    }
}
