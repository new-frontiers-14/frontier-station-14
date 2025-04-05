using Robust.Shared.Prototypes;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Enums;
using Content.Shared.Roles;
using Content.Shared.Ghost;

namespace Content.Server._NF.Players;

public sealed class JobPresentSystem : EntitySystem
{
    [Dependency] private readonly SharedJobSystem _jobs = default!;

    /// <summary>
    /// Returns the number of active players who match the requested Job Prototype Id.
    /// </summary>
    /// <param name="jobProtoId">PrototypeID for a job to check.</param>
    /// <returns>the number of active players with this job.</returns>
    public int getNumberOfActiveRoles(ProtoId<JobPrototype> jobProtoId)
    {
        var activeJobCount = 0;
        var jobQuery = AllEntityQuery<JobRoleComponent, MindRoleComponent>();
        while (jobQuery.MoveNext(out _, out _, out var mindRole))
        {
            if (!_jobs.MindHasJobWithId(mindRole.Mind, jobProtoId)) // Skip if the job doesn't match
                continue;

            if (!TryComp(mindRole.Mind.Comp.CurrentEntity, out TransformComponent? xform) || xform.MapUid == null) // Skip if they're in nullspace
                continue;

            if (GetNetEntity(mindRole.Mind.Comp.CurrentEntity) != mindRole.Mind.Comp.OriginalOwnedEntity) // Skip if they're no longer in their original body (ghost or ghostrole)
                continue;

            if (mindRole.Mind.Comp.Session?.State.Status != SessionStatus.InGame) // Skip if they're SSD
                continue;

            activeJobCount++;
        }
        return activeJobCount;
    }
}