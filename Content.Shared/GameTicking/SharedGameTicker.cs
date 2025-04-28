using System.Linq;
using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Replays;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Timing;
using Robust.Shared.Audio;
using Robust.Shared.Utility;
using Content.Shared._NF.Shipyard.Prototypes; // Frontier

namespace Content.Shared.GameTicking
{
    public abstract class SharedGameTicker : EntitySystem
    {
        [Dependency] private readonly IReplayRecordingManager _replay = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        // See ideally these would be pulled from the job definition or something.
        // But this is easier, and at least it isn't hardcoded.
        //TODO: Move these, they really belong in StationJobsSystem or a cvar.
        [ValidatePrototypeId<JobPrototype>]
        public const string FallbackOverflowJob = "Contractor"; // Frontier: Passenger<Contractor

        public const string FallbackOverflowJobName = "job-name-contractor"; // Frontier: job-name-passenger<job-name-contractor

        // TODO network.
        // Probably most useful for replays, round end info, and probably things like lobby menus.
        [ViewVariables]
        public int RoundId { get; protected set; }
        [ViewVariables] public TimeSpan RoundStartTimeSpan { get; protected set; }

        public override void Initialize()
        {
            base.Initialize();
            _replay.RecordingStarted += OnRecordingStart;
        }

        public override void Shutdown()
        {
            _replay.RecordingStarted -= OnRecordingStart;
        }

        private void OnRecordingStart(MappingDataNode metadata, List<object> events)
        {
            if (RoundId != 0)
            {
                metadata["roundId"] = new ValueDataNode(RoundId.ToString());
            }
        }

        public TimeSpan RoundDuration()
        {
            return _gameTiming.CurTime.Subtract(RoundStartTimeSpan);
        }
    }

    [Serializable, NetSerializable]
    public sealed class TickerJoinLobbyEvent : EntityEventArgs
    {
    }

    [Serializable, NetSerializable]
    public sealed class TickerJoinGameEvent : EntityEventArgs
    {
    }

    [Serializable, NetSerializable]
    public sealed class TickerLateJoinStatusEvent : EntityEventArgs
    {
        // TODO: Make this a replicated CVar, honestly.
        public bool Disallowed { get; }

        public TickerLateJoinStatusEvent(bool disallowed)
        {
            Disallowed = disallowed;
        }
    }

    [Serializable, NetSerializable]
    public sealed class TickerConnectionStatusEvent : EntityEventArgs
    {
        public TimeSpan RoundStartTimeSpan { get; }
        public TickerConnectionStatusEvent(TimeSpan roundStartTimeSpan)
        {
            RoundStartTimeSpan = roundStartTimeSpan;
        }
    }

    [Serializable, NetSerializable]
    public sealed class TickerLobbyStatusEvent : EntityEventArgs
    {
        public bool IsRoundStarted { get; }
        public string? LobbyBackground { get; }
        public bool YouAreReady { get; }
        // UTC.
        public TimeSpan StartTime { get; }
        public TimeSpan RoundStartTimeSpan { get; }
        public bool Paused { get; }

        public TickerLobbyStatusEvent(bool isRoundStarted, string? lobbyBackground, bool youAreReady, TimeSpan startTime, TimeSpan preloadTime, TimeSpan roundStartTimeSpan, bool paused)
        {
            IsRoundStarted = isRoundStarted;
            LobbyBackground = lobbyBackground;
            YouAreReady = youAreReady;
            StartTime = startTime;
            RoundStartTimeSpan = roundStartTimeSpan;
            Paused = paused;
        }
    }

    [Serializable, NetSerializable]
    public sealed class TickerLobbyInfoEvent : EntityEventArgs
    {
        public string TextBlob { get; }

        public TickerLobbyInfoEvent(string textBlob)
        {
            TextBlob = textBlob;
        }
    }

    [Serializable, NetSerializable]
    public sealed class TickerLobbyCountdownEvent : EntityEventArgs
    {
        /// <summary>
        /// The game time that the game will start at.
        /// </summary>
        public TimeSpan StartTime { get; }

        /// <summary>
        /// Whether or not the countdown is paused
        /// </summary>
        public bool Paused { get; }

        public TickerLobbyCountdownEvent(TimeSpan startTime, bool paused)
        {
            StartTime = startTime;
            Paused = paused;
        }
    }

    // Frontier: station job info, optional structs
    /// <summary>
    /// General job information for each station-like entity (both stations and shuttles)
    /// </summary>
    /// <param name="stationName">The name of the station.</param>
    /// <param name="jobsAvailable">A dictionary of job prototypes and the number of jobs positions available for it.</param>
    /// <param name="isLateJoinStation">If true, this entity is a station, and not a player ship (displayed under the "Crew" tab).</param>
    [Serializable, NetSerializable]
    public sealed class StationJobInformation(
        string stationName,
        Dictionary<ProtoId<JobPrototype>, int?> jobsAvailable,
        bool isLateJoinStation,
        StationDisplayInformation? stationDisplayInfo,
        VesselDisplayInformation? vesselDisplayInfo
        )
    {
        public string StationName { get; } = stationName;
        public Dictionary<ProtoId<JobPrototype>, int?> JobsAvailable { get; } = jobsAvailable;
        public bool IsLateJoinStation { get; } = isLateJoinStation;
        public StationDisplayInformation? StationDisplayInfo { get; } = stationDisplayInfo;
        public VesselDisplayInformation? VesselDisplayInformation { get; } = vesselDisplayInfo;
    }

    /// <summary>
    /// Additional optional station-specific fields.
    /// </summary>
    /// <param name="stationSubtext">The subtext that is shown under the station name.</param>
    /// <param name="stationDescription">A longer description of the station, describing what the player can
    /// do there</param>
    /// <param name="stationIcon">The icon that represents the station and is shown next to the name.</param>
    /// <param name="lobbySortOrder">The order in which this station should be displayed in the station picker.</param>
    [Serializable, NetSerializable]
    public sealed class StationDisplayInformation(
        LocId? stationSubtext,
        LocId? stationDescription,
        ResPath? stationIcon,
        int lobbySortOrder
        )
    {
        public LocId? StationSubtext { get; } = stationSubtext;
        public LocId? StationDescription { get; } = stationDescription;
        public ResPath? StationIcon { get; } = stationIcon;
        public int LobbySortOrder { get; } = lobbySortOrder;
    }

    /// <summary>
    /// Additional optional vessel-specific fields.
    /// </summary>
    /// <param name="vesselAdvertisement">A player-input string advertising the ship to other players.</param>
    /// <param name="vessel">The prototype ID for the vessel this ship is.</param>
    /// <param name="hiddenIfNoJobs">If true, this vessel should be hidden when there are no open jobs on it.</param>
    [Serializable, NetSerializable]
    public sealed class VesselDisplayInformation(
        string vesselAdvertisement,
        ProtoId<VesselPrototype>? vessel,
        bool hiddenIfNoJobs
        )
    {
        public string VesselAdvertisement { get; } = vesselAdvertisement;
        public ProtoId<VesselPrototype>? Vessel { get; } = vessel;
        public bool HiddenIfNoJobs { get; } = hiddenIfNoJobs;
    }
    // End Frontier: station job info, optional structs

    [Serializable, NetSerializable]
    public sealed class TickerJobsAvailableEvent(
        Dictionary<NetEntity, StationJobInformation> stationJobList // Frontier addition, replaced with StationJobInformation
    ) : EntityEventArgs
    {
        public Dictionary<NetEntity, StationJobInformation> StationJobList { get; } = stationJobList;
    }

    [Serializable, NetSerializable, DataDefinition]
    public sealed partial class RoundEndMessageEvent : EntityEventArgs
    {
        [Serializable, NetSerializable, DataDefinition]
        public partial struct RoundEndPlayerInfo
        {
            [DataField]
            public string PlayerOOCName;

            [DataField]
            public string? PlayerICName;

            [DataField, NonSerialized]
            public NetUserId? PlayerGuid;

            public string Role;

            [DataField, NonSerialized]
            public string[] JobPrototypes;

            [DataField, NonSerialized]
            public string[] AntagPrototypes;

            public NetEntity? PlayerNetEntity;

            [DataField]
            public bool Antag;

            [DataField]
            public bool Observer;

            public bool Connected;
        }

        public string GamemodeTitle { get; }
        public string RoundEndText { get; }
        public TimeSpan RoundDuration { get; }
        public int RoundId { get; }
        public int PlayerCount { get; }
        public RoundEndPlayerInfo[] AllPlayersEndInfo { get; }

        /// <summary>
        /// Sound gets networked due to how entity lifecycle works between client / server and to avoid clipping.
        /// </summary>
        public ResolvedSoundSpecifier? RestartSound;

        public RoundEndMessageEvent(
            string gamemodeTitle,
            string roundEndText,
            TimeSpan roundDuration,
            int roundId,
            int playerCount,
            RoundEndPlayerInfo[] allPlayersEndInfo,
            ResolvedSoundSpecifier? restartSound)
        {
            GamemodeTitle = gamemodeTitle;
            RoundEndText = roundEndText;
            RoundDuration = roundDuration;
            RoundId = roundId;
            PlayerCount = playerCount;
            AllPlayersEndInfo = allPlayersEndInfo;
            RestartSound = restartSound;
        }
    }

    [Serializable, NetSerializable]
    public enum PlayerGameStatus : sbyte
    {
        NotReadyToPlay = 0,
        ReadyToPlay,
        JoinedGame,
    }
}
