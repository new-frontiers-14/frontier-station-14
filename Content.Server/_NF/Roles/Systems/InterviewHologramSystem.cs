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
using Robust.Shared.Player;

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
    [Dependency] private JobTrackingSystem _jobTracking = default!;
    [Dependency] private ActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InterviewHologramComponent, MapInitEvent>(OnHologramMapInit);
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

        // Apply the current character's appearance from their profile, if it exists.
        if (!TryComp(ent, out MindContainerComponent? mindContainer)
            || !_mind.TryGetSession(mindContainer.Mind, out var session))
            return;

        ApplyAppearanceForSession(ent, session);
    }

    private void OnHologramMindRemoved(Entity<InterviewHologramComponent> ent, ref MindRemovedMessage ev)
    {
        // Don't let holograms linger.
        QueueDel(ent);
    }

    private void OnHologramMindAdded(Entity<InterviewHologramComponent> ent, ref MindAddedMessage ev)
    {
        // Apply the current character's appearance from their profile, if it exists and hasn't already been applied.
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
        if (_mind.TryGetSession(_mind.GetMind(ent), out var session))
            _gameTicker.Respawn(session);

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
        if (_mind.TryGetSession(_mind.GetMind(ent), out var session))
            _gameTicker.Respawn(session);

        // Ensure our job slot reopens
        if (TryComp<JobTrackingComponent>(ent, out var jobTracking))
            _jobTracking.OpenJob((ent.Owner, jobTracking));

        QueueDel(ent);
    }
}
