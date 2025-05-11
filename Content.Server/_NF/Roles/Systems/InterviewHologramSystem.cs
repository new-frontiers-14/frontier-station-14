using Content.Server.Actions;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.PDA.Ringer;
using Content.Server.Preferences.Managers;
using Content.Server.Station.Systems;
using Content.Shared._NF.Roles.Components;
using Content.Shared._NF.Roles.Events;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.PDA;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.Verbs;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._NF.Roles.Systems;

public sealed class InterviewHologramSystem : SharedInterviewHologramSystem
{
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private GameTicker _gameTicker = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IServerPreferencesManager _prefs = default!;
    [Dependency] private ActionsSystem _actions = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private MetaDataSystem _meta = default!;
    [Dependency] private RingerSystem _ringer = default!;
    [Dependency] private SharedHumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedRoleSystem _roles = default!;
    [Dependency] private StationJobsSystem _stationJobs = default!;
    [Dependency] private StationSpawningSystem _stationSpawning = default!;
    [Dependency] private StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InterviewHologramComponent, PlayerSpawnCompleteEvent>(OnHologramPlayerSpawnComplete);
        SubscribeLocalEvent<InterviewHologramComponent, MapInitEvent>(OnHologramMapInit);
        SubscribeLocalEvent<InterviewHologramComponent, GetVerbsEvent<AlternativeVerb>>(OnAlternativeVerb);
        SubscribeLocalEvent<InterviewHologramComponent, MindRemovedMessage>(OnHologramMindRemoved);
        SubscribeLocalEvent<InterviewHologramComponent, MindAddedMessage>(OnHologramMindAdded);
        SubscribeLocalEvent<InterviewHologramComponent, CancelInterviewEvent>(OnHologramCancelInterview);
        SubscribeLocalEvent<InterviewHologramComponent, DismissInterviewEvent>(OnHologramDismissInterview);
    }

    private void OnHologramPlayerSpawnComplete(Entity<InterviewHologramComponent> ent, ref PlayerSpawnCompleteEvent ev)
    {
        ent.Comp.Station = ev.Station;
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
            Act = () => RaiseLocalEvent(ent, new DismissInterviewEvent(captain, true)),
            Text = Loc.GetString("interview-hologram-dismiss"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/delete_transparent.svg.192dpi.png")),
            Priority = -1
        });
        ev.Verbs.Add(new AlternativeVerb()
        {
            Act = () => RaiseLocalEvent(ent, new DismissInterviewEvent(captain, false)),
            Text = Loc.GetString("interview-hologram-dismiss-and-close"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/delete_transparent.svg.192dpi.png")),
            Priority = -2
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
        // Nothing to do.
        if (ent.Comp.AppearanceApplied && ent.Comp.NotificationsSent
            || !TryComp(ent, out MindContainerComponent? mindContainer)
            || !_mind.TryGetSession(mindContainer.Mind, out var session))
        {
            return;
        }

        // Apply the current character's appearance from their profile if it exists and hasn't already been applied
        if (!ent.Comp.AppearanceApplied)
        {
            ApplyAppearanceForSession(ent, session);
        }

        // Notify all relevant captains if they have their PDA that someone is applying for a job. 
        if (!ent.Comp.NotificationsSent)
        {
            string jobTitle;
            if (_proto.TryIndex(ent.Comp.Job, out var jobProto))
                jobTitle = jobProto.LocalizedName;
            else
                jobTitle = Loc.GetString("interview-notification-default-job");

            var msgString = Loc.GetString("interview-hologram-pda-notification", ("applicant", ent), ("jobTitle", jobTitle));
            var message = FormattedMessage.EscapeText(msgString);
            var wrappedMessage = Loc.GetString("pda-notification-message",
                ("header", Loc.GetString("interview-notification-pda-header")),
                ("message", message));

            // Find all people that might receive this message.
            var mindQuery = EntityQueryEnumerator<MindComponent>();
            while (mindQuery.MoveNext(out _, out var mindComp))
            {
                if (mindComp.CurrentEntity == null
                    || mindComp.Session == null
                    || !_inventory.TryGetSlotEntity(mindComp.CurrentEntity.Value, "id", out var slotItem)
                    || !HasComp<PdaComponent>(slotItem)
                    || !IsCaptain(mindComp.CurrentEntity.Value, ent))
                {
                    continue;
                }

                _ringer.RingerPlayRingtone(slotItem.Value);

                _chat.ChatMessageToOne(
                    ChatChannel.Notifications,
                    message,
                    wrappedMessage,
                    EntityUid.Invalid,
                    false,
                    mindComp.Session.Channel);
            }

            ent.Comp.NotificationsSent = true;
        }
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

        HumanoidCharacterProfile profile;
        if (_prefs.GetPreferences(session.UserId).SelectedCharacter is HumanoidCharacterProfile currentProfile)
            profile = currentProfile;
        else
            profile = HumanoidCharacterProfile.Random();

        // Prevent reopening the applicant's slot.
        RemComp<JobTrackingComponent>(ent);

        // Spawn and inhabit new entity, tell them they got the job.
        var newEntity = _stationSpawning.SpawnPlayerMob(xform.Coordinates,
            ent.Comp.Job,
            profile,
            ent.Comp.Station,
            entity: null,
            session: session
            );

        _mind.TransferTo(mindUid.Value, newEntity);
        _chat.DispatchServerMessage(session, Loc.GetString("interview-hologram-message-accepted"), suppressLog: true);
        _roles.MindAddJobRole(mindUid.Value, jobPrototype: ent.Comp.Job); // Overwrites

        // Run spawn event for game rules, traits, etc.
        _gameTicker.PlayersJoinedRoundNormally++;
        var aev = new PlayerSpawnCompleteEvent(newEntity,
            session,
            ent.Comp.Job,
            lateJoin: true,
            silent: true,
            joinOrder: _gameTicker.PlayersJoinedRoundNormally, // Increment regardless (unused as of writing)
            ent.Comp.Station,
            profile);
        RaiseLocalEvent(newEntity, aev, true);

        // Log the acceptance.
        string stationName;
        if (TryComp(ent.Comp.Station, out MetaDataComponent? meta))
            stationName = $"station {meta.EntityName:stationName}";
        else
            stationName = "an unknown station";

        _adminLogger.Add(LogType.LateJoin,
            LogImpact.Medium,
            $"Player {session.Name} controlling {ToPrettyString(ent):entity} has been spawned via interview on {stationName} as a {ent.Comp.Job:jobName}.");

        // Delete the old hologram.
        QueueDel(ent);
    }

    private void OnHologramCancelInterview(Entity<InterviewHologramComponent> ent, ref CancelInterviewEvent ev)
    {
        // Log cancellation
        string player;
        if (_playerManager.TryGetSessionByEntity(ent, out var session))
            player = $"Player {session.Name}";
        else
            player = $"Someone";

        var stationUid = _station.GetOwningStation(ent);
        string station;
        if (stationUid != null && TryComp(stationUid, out MetaDataComponent? meta))
            station = $"station {meta.EntityName:stationName}";
        else
            station = "an unknown station";

        _adminLogger.Add(LogType.LateJoin,
            LogImpact.Medium,
            $"{player} controlling {ToPrettyString(ent):entity} cancelled their interview on {station} for a {ent.Comp.Job:jobName} position.");

        // Run dismissal
        DismissHologram(ent, message: Loc.GetString("interview-hologram-message-cancelled"));
    }

    private void OnHologramDismissInterview(Entity<InterviewHologramComponent> ent, ref DismissInterviewEvent ev)
    {
        // Log cancellation
        string player;
        if (_playerManager.TryGetSessionByEntity(ent, out var session))
            player = $"Player {session.Name}";
        else
            player = $"Someone";

        var stationUid = _station.GetOwningStation(ent);
        string station;
        if (stationUid != null && TryComp(stationUid, out MetaDataComponent? meta))
            station = $"station {meta.EntityName:stationName}";
        else
            station = "an unknown station";

        _adminLogger.Add(LogType.LateJoin,
            LogImpact.Medium,
            $"{player} controlling {ToPrettyString(ev.Dismisser):entity} dismissed {ToPrettyString(ent):entity} from their interview on {station} for a {ent.Comp.Job:jobName} position.");

        // Run dismissal
        DismissHologram(ent, ev.ReopenSlot, message: Loc.GetString("interview-hologram-message-dismissed"));
    }

    private void DismissHologram(Entity<InterviewHologramComponent> ent, bool reopenSlot = true, string? message = null)
    {
        // Override job tracking - explicitly reopen the job slot, whatever it was.
        if (TryComp<JobTrackingComponent>(ent, out var jobTracking))
        {
            if (jobTracking.Job != null && reopenSlot)
                _stationJobs.TryAdjustJobSlot(jobTracking.SpawnStation, jobTracking.Job, 1);
            RemComp<JobTrackingComponent>(ent);
        }

        if (_mind.TryGetSession(_mind.GetMind(ent), out var session))
        {
            // Inform the user why they were dismissed.
            if (message != null)
                _chat.DispatchServerMessage(session, message, suppressLog: true);

            _gameTicker.Respawn(session);
        }

        QueueDel(ent);
    }
}
