using Content.Server.Actions;
using Content.Server.GameTicking;
using Content.Server.Preferences.Managers;
using Content.Server.Station.Systems;
using Content.Shared._NF.Roles.Components;
using Content.Shared._NF.Roles.Events;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Preferences;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server._NF.Roles.Systems;

public sealed class InterviewHologramSystem : SharedInterviewHologramSystem
{
    [Dependency] private SharedHumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private GameTicker _gameTicker = default!;
    [Dependency] private MetaDataSystem _meta = default!;
    [Dependency] private StationSpawningSystem _stationSpawning = default!;
    [Dependency] private IServerPreferencesManager _prefs = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private StationJobsSystem _stationJobs = default!;
    [Dependency] private ActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InterviewHologramComponent, MapInitEvent>(OnHologramMapInit);
        SubscribeLocalEvent<InterviewHologramComponent, GetVerbsEvent<AlternativeVerb>>(OnAlternativeVerb);
        SubscribeLocalEvent<InterviewHologramComponent, MindRemovedMessage>(OnHologramMindRemoved);
        SubscribeLocalEvent<InterviewHologramComponent, MindAddedMessage>(OnHologramMindAdded);
        SubscribeLocalEvent<InterviewHologramComponent, CancelInterviewEvent>(OnHologramCancelInterview);
        SubscribeLocalEvent<InterviewHologramComponent, DismissInterviewEvent>(OnHologramDismissInterview);
    }

    private void OnHologramMapInit(Entity<InterviewHologramComponent> ent, ref MapInitEvent ev)
    {
        _actions.AddAction(ent, ref ent.Comp.CancelApplicationActionEntity, ent.Comp.CancelApplicationAction);
        _actions.AddAction(ent, ref ent.Comp.ToggleApprovalActionEntity, ent.Comp.ToggleApprovalAction);
        _actions.SetToggled(ent.Comp.ToggleApprovalActionEntity, ent.Comp.ApplicantApproved);

        // Apply the current character's appearance from their profile if it exists.
        if (!TryComp(ent, out MindContainerComponent? mindContainer)
            || !_mind.TryGetSession(mindContainer.Mind, out var session))
            return;

        ApplyAppearanceForSession(ent, session);
    }

    // FIXME: This is currently on the server because ShuttleDeed isn't currently properly networked to the client.
    private void OnAlternativeVerb(Entity<InterviewHologramComponent> ent, ref GetVerbsEvent<AlternativeVerb> ev)
    {
        // No access/interact check, should be possible with sight alone
        if (ev.Hands == null || ev.User == ev.Target)
            return;

        bool accepted = ent.Comp.CaptainApproved;
        EntityUid captain = ev.User;
        bool isCaptain = IsCaptain(ev.User, ent);
        ev.Verbs.Add(new AlternativeVerb()
        {
            Act = () => RaiseLocalEvent(ent, new SetCaptainApprovedEvent(captain, !accepted)),
            Text = Loc.GetString(accepted ? "interview-hologram-rescind" : "interview-hologram-approve"),
            Icon = new SpriteSpecifier.Texture(new(accepted ? "/Textures/_NF/Interface/VerbIcons/cross.png" : "/Textures/_NF/Interface/VerbIcons/check.png")),
            Disabled = !isCaptain,
            Message = isCaptain ? null : Loc.GetString("interview-hologram-verb-message-need-deed")
        });
        ev.Verbs.Add(new AlternativeVerb()
        {
            Act = () => RaiseLocalEvent(ent, new DismissInterviewEvent(captain)),
            Text = Loc.GetString("interview-hologram-dismiss"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/delete.svg.192dpi.png")),
            Disabled = !isCaptain,
            Message = isCaptain ? null : Loc.GetString("interview-hologram-verb-message-need-deed"),
            Priority = -1
        });
    }

    private void OnHologramMindRemoved(Entity<InterviewHologramComponent> ent, ref MindRemovedMessage ev)
    {
        // Override job tracking - explicitly reopen the job slot, whatever it was.
        if (TryComp<JobTrackingComponent>(ent, out var jobTracking))
        {
            if (jobTracking.Job != null)
                _stationJobs.TryAdjustJobSlot(jobTracking.SpawnStation, jobTracking.Job, 1);
            RemComp<JobTrackingComponent>(ent);
        }

        // Don't let holograms linger.
        QueueDel(ent);
    }

    private void OnHologramMindAdded(Entity<InterviewHologramComponent> ent, ref MindAddedMessage ev)
    {
        // Apply the current character's appearance from their profile if it exists and hasn't already been applied
        if (ent.Comp.AppearanceApplied
            || !TryComp(ent, out MindContainerComponent? mindContainer)
            || !_mind.TryGetSession(mindContainer.Mind, out var session))
            return;

        ApplyAppearanceForSession(ent, session);
    }

    private void ApplyAppearanceForSession(Entity<InterviewHologramComponent> ent, ICommonSession session)
    {
        var profile = _gameTicker.GetPlayerProfile(session);
        _humanoid.LoadProfile(ent, profile);
        _meta.SetEntityName(ent, profile.Name);
        ent.Comp.AppearanceApplied = true;
    }

    protected override void HandleApprovalChanged(Entity<InterviewHologramComponent> ent)
    {
        // Need both approvals to actually spawn.
        if (!ent.Comp.ApplicantApproved || !ent.Comp.CaptainApproved)
            return;

        // Entity must have a valid set of coordinates.
        if (!TryComp(ent, out TransformComponent? xform))
            return;

        var mindUid = _mind.GetMind(ent);
        if (mindUid == null || !_mind.TryGetSession(mindUid, out var session))
            return;

        HumanoidCharacterProfile? profile = null;
        if (_prefs.GetPreferences(session.UserId).SelectedCharacter is HumanoidCharacterProfile currentProfile)
            profile = currentProfile;

        // Prevent reopening the applicant's slot.
        RemComp<JobTrackingComponent>(ent);

        // Spawn new entity.
        var newEntity = _stationSpawning.SpawnPlayerMob(xform.Coordinates,
            ent.Comp.Job,
            profile,
            _station.GetOwningStation(ent),
            entity: null,
            session: session
            );

        _mind.TransferTo(mindUid.Value, newEntity);

        // Delete the old hologram.
        QueueDel(ent);
    }

    private void OnHologramCancelInterview(Entity<InterviewHologramComponent> ent, ref CancelInterviewEvent ev)
    {
        DismissHologram(ent);
    }

    private void OnHologramDismissInterview(Entity<InterviewHologramComponent> ent, ref DismissInterviewEvent ev)
    {
        if (!IsCaptain(ev.Captain, ent))
            return;

        DismissHologram(ent);
    }

    private void DismissHologram(Entity<InterviewHologramComponent> ent)
    {
        // Override job tracking - explicitly reopen the job slot, whatever it was.
        if (TryComp<JobTrackingComponent>(ent, out var jobTracking))
        {
            if (jobTracking.Job != null)
                _stationJobs.TryAdjustJobSlot(jobTracking.SpawnStation, jobTracking.Job, 1);
            RemComp<JobTrackingComponent>(ent);
        }

        if (_mind.TryGetSession(_mind.GetMind(ent), out var session))
            _gameTicker.Respawn(session);

        QueueDel(ent);
    }
}
