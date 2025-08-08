using Content.Server._NF.CryoSleep;
using Content.Server.Afk;
using Content.Server.GameTicking;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared._NF.Roles.Components;
using Content.Shared._NF.Roles.Systems;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.Roles.Systems;

/// <summary>
/// This handles job tracking for station jobs that should be reopened on cryo.
/// </summary>
public sealed class JobTrackingSystem : SharedJobTrackingSystem
{
    [Dependency] private readonly IAfkManager _afk = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly StationJobsSystem _stationJobs = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JobTrackingComponent, CryosleepBeforeMindRemovedEvent>(OnJobBeforeCryoEntered);
        SubscribeLocalEvent<JobTrackingComponent, MindAddedMessage>(OnJobMindAdded);
        SubscribeLocalEvent<JobTrackingComponent, MindRemovedMessage>(OnJobMindRemoved);
    }

    // If, through admin jiggery pokery, the player returns (or the mob is controlled), we should close the slot if it's opened.
    private void OnJobMindAdded(Entity<JobTrackingComponent> ent, ref MindAddedMessage ev)
    {
        if (ent.Comp.Job is not { } job || ent.Comp.Active)
            return;

        ent.Comp.Active = true;

        if (!JobShouldBeReopened(ent.Comp.Job.Value))
            return;

        try
        {
            if (!TryComp<StationJobsComponent>(ent.Comp.SpawnStation, out var stationJobs)
                || !_stationJobs.TryGetJobSlot(ent.Comp.SpawnStation, job, out var slots)
                || slots == null)
                return;

            // The character is back, readjust their job slot if you can.
            _stationJobs.TryAdjustJobSlot(ent.Comp.SpawnStation, job, -1);
        }
        catch (ArgumentException)
        {
        }
        catch (KeyNotFoundException)
        {
        }
    }

    private void OnJobMindRemoved(Entity<JobTrackingComponent> ent, ref MindRemovedMessage ev)
    {
        if (ent.Comp.Job == null || !ent.Comp.Active || !JobShouldBeReopened(ent.Comp.Job.Value))
            return;

        OpenJob(ent);
    }

    private void OnJobBeforeCryoEntered(Entity<JobTrackingComponent> ent, ref CryosleepBeforeMindRemovedEvent ev)
    {
        if (ent.Comp.Job == null || !ent.Comp.Active || !JobShouldBeReopened(ent.Comp.Job.Value))
            return;

        OpenJob(ent);
        ev.DeleteEntity = true;
    }

    public void OpenJob(Entity<JobTrackingComponent> ent)
    {
        if (ent.Comp.Job is not { } job)
            return;

        if (!TryComp<StationJobsComponent>(ent.Comp.SpawnStation, out var stationJobs))
            return;

        ent.Comp.Active = false;

        try
        {
            if (!_stationJobs.TryGetJobSlot(ent.Comp.SpawnStation, job, out var slots)
                || slots == null)
                return;

            // Get number of open job slots that are present (not on the cryo map [or on expedition]).
            var occupiedJobs = GetNumberOfActiveRoles(job, includeAfk: true, exclude: ent);

            if (slots + occupiedJobs >= stationJobs.SetupAvailableJobs[job][1])
                return;

            _stationJobs.TryAdjustJobSlot(ent.Comp.SpawnStation, job, 1);
        }
        catch (ArgumentException)
        {
        }
        catch (KeyNotFoundException)
        {
        }
    }

    /// <summary>
    /// Returns the number of active players who match the requested Job Prototype Id.
    /// </summary>
    /// <param name="jobProtoId">PrototypeID for a job to check.</param>
    /// <param name="includeAfk">If true, includes AFK players in the check.</param>
    /// <returns>The number of active players with this job.</returns>
    public int GetNumberOfActiveRoles(ProtoId<JobPrototype> jobProtoId, bool includeAfk = true, EntityUid? exclude = null)
    {
        var activeJobCount = 0;
        var jobQuery = AllEntityQuery<JobTrackingComponent, MindContainerComponent, TransformComponent>();
        while (jobQuery.MoveNext(out var uid, out var job, out var mindContainer, out var xform))
        {
            if (exclude == uid)
                continue;

            if (!job.Active
                || job.Job != jobProtoId
                || xform.MapID != _gameTicker.DefaultMap // Skip if they're in cryo or on expedition
                || !_player.TryGetSessionByEntity(uid, out var session)
                || session.State.Status != SessionStatus.InGame)
                continue;

            if (!includeAfk && _afk.IsAfk(session))
                continue;

            activeJobCount++;
        }
        return activeJobCount;
    }
}
