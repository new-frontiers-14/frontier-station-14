using Content.Client.Gameplay;
using Content.Client.Lobby;
using Content.Client.RoundEnd;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.GameWindow;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.State;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Client.GameTicking.Managers
{
    [UsedImplicitly]
    public sealed class ClientGameTicker : SharedGameTicker
    {
        [Dependency] private readonly IStateManager _stateManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;

        [ViewVariables] private bool _initialized;
        private Dictionary<NetEntity, Dictionary<string, uint?>>  _jobsAvailable = new();
        private Dictionary<NetEntity, string> _stationNames = new();

        /// <summary>
        /// The current round-end window. Could be used to support re-opening the window after closing it.
        /// </summary>
        private RoundEndSummaryWindow? _window;

        [ViewVariables] public bool AreWeReady { get; private set; }
        [ViewVariables] public bool IsGameStarted { get; private set; }
        [ViewVariables] public string? LobbySong { get; private set; }
        [ViewVariables] public string? RestartSound { get; private set; }
        [ViewVariables] public string? LobbyBackground { get; private set; }
        [ViewVariables] public bool DisallowedLateJoin { get; private set; }
        [ViewVariables] public string? ServerInfoBlob { get; private set; }
        [ViewVariables] public TimeSpan StartTime { get; private set; }
        [ViewVariables] public TimeSpan RoundStartTimeSpan { get; private set; }
        [ViewVariables] public new bool Paused { get; private set; }

        [ViewVariables] public IReadOnlyDictionary<NetEntity, Dictionary<string, uint?>> JobsAvailable => _jobsAvailable;
        [ViewVariables] public IReadOnlyDictionary<NetEntity, string> StationNames => _stationNames;

        public event Action? InfoBlobUpdated;
        public event Action? LobbyStatusUpdated;
        public event Action? LobbySongUpdated;
        public event Action? LobbyLateJoinStatusUpdated;
        public event Action<IReadOnlyDictionary<NetEntity, Dictionary<string, uint?>>>? LobbyJobsAvailableUpdated;

        public override void Initialize()
        {
            DebugTools.Assert(!_initialized);

            SubscribeNetworkEvent<TickerJoinLobbyEvent>(JoinLobby);
            SubscribeNetworkEvent<TickerJoinGameEvent>(JoinGame);
            SubscribeNetworkEvent<TickerLobbyStatusEvent>(LobbyStatus);
            SubscribeNetworkEvent<TickerLobbyInfoEvent>(LobbyInfo);
            SubscribeNetworkEvent<TickerLobbyCountdownEvent>(LobbyCountdown);
            SubscribeNetworkEvent<RoundEndMessageEvent>(RoundEnd);
            SubscribeNetworkEvent<RequestWindowAttentionEvent>(msg =>
            {
                IoCManager.Resolve<IClyde>().RequestWindowAttention();
            });
            SubscribeNetworkEvent<TickerLateJoinStatusEvent>(LateJoinStatus);
            SubscribeNetworkEvent<TickerJobsAvailableEvent>(UpdateJobsAvailable);
            SubscribeNetworkEvent<RoundRestartCleanupEvent>(RoundRestartCleanup);

            _initialized = true;
        }

        public void SetLobbySong(string? song, bool forceUpdate = false)
        {
            var updated = song != LobbySong;

            LobbySong = song;

            if (updated || forceUpdate)
                LobbySongUpdated?.Invoke();
        }

        private void LateJoinStatus(TickerLateJoinStatusEvent message)
        {
            DisallowedLateJoin = message.Disallowed;
            LobbyLateJoinStatusUpdated?.Invoke();
        }

        private void UpdateJobsAvailable(TickerJobsAvailableEvent message)
        {
            _jobsAvailable.Clear();

            foreach (var (job, data) in message.JobsAvailableByStation)
            {
                _jobsAvailable[job] = data;
            }

            _stationNames.Clear();
            foreach (var weh in message.StationNames)
            {
                _stationNames[weh.Key] = weh.Value;
            }

            LobbyJobsAvailableUpdated?.Invoke(JobsAvailable);
        }

        private void JoinLobby(TickerJoinLobbyEvent message)
        {
            _stateManager.RequestStateChange<LobbyState>();
        }

        private void LobbyStatus(TickerLobbyStatusEvent message)
        {
            StartTime = message.StartTime;
            RoundStartTimeSpan = message.RoundStartTimeSpan;
            IsGameStarted = message.IsRoundStarted;
            AreWeReady = message.YouAreReady;
            SetLobbySong(message.LobbySong);
            LobbyBackground = message.LobbyBackground;
            Paused = message.Paused;

            LobbyStatusUpdated?.Invoke();
        }

        private void LobbyInfo(TickerLobbyInfoEvent message)
        {
            ServerInfoBlob = message.TextBlob;

            InfoBlobUpdated?.Invoke();
        }

        private void JoinGame(TickerJoinGameEvent message)
        {
            _stateManager.RequestStateChange<GameplayState>();
        }

        private void LobbyCountdown(TickerLobbyCountdownEvent message)
        {
            StartTime = message.StartTime;
            Paused = message.Paused;
        }

        private void RoundEnd(RoundEndMessageEvent message)
        {
            // Force an update in the event of this song being the same as the last.
            SetLobbySong(message.LobbySong, true);
            RestartSound = message.RestartSound;

            // Don't open duplicate windows (mainly for replays).
            if (_window?.RoundId == message.RoundId)
                return;

            //This is not ideal at all, but I don't see an immediately better fit anywhere else.
            _window = new RoundEndSummaryWindow(message.GamemodeTitle, message.RoundEndText, message.RoundDuration, message.RoundId, message.AllPlayersEndInfo, _entityManager);
        }

        private void RoundRestartCleanup(RoundRestartCleanupEvent ev)
        {
            if (string.IsNullOrEmpty(RestartSound))
                return;

            if (!_configManager.GetCVar(CCVars.RestartSoundsEnabled))
            {
                RestartSound = null;
                return;
            }

            _audio.PlayGlobal(RestartSound, Filter.Local(), false);

            // Cleanup the sound, we only want it to play when the round restarts after it ends normally.
            RestartSound = null;
        }
    }
}
