using System.Linq;
using Content.Client.Administration.Managers;
using Content.Client.Gameplay;
using Content.Client.Lobby;
using Content.Client.RoundEnd;
using Content.Shared.GameTicking;
using Content.Shared.GameWindow;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.State;
using Robust.Client.UserInterface;

namespace Content.Client.GameTicking.Managers
{
    [UsedImplicitly]
    public sealed class ClientGameTicker : SharedGameTicker
    {
        [Dependency] private readonly IStateManager _stateManager = default!;
        [Dependency] private readonly IClientAdminManager _admin = default!;
        [Dependency] private readonly IClyde _clyde = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;

        private Dictionary<NetEntity, StationJobInformation> _stationJobInformationList = new();

        [ViewVariables] public bool AreWeReady { get; private set; }
        [ViewVariables] public bool IsGameStarted { get; private set; }
        [ViewVariables] public string? RestartSound { get; private set; }
        [ViewVariables] public string? LobbyBackground { get; private set; }
        [ViewVariables] public bool DisallowedLateJoin { get; private set; }
        [ViewVariables] public string? ServerInfoBlob { get; private set; }
        [ViewVariables] public TimeSpan StartTime { get; private set; }
        [ViewVariables] public new bool Paused { get; private set; }

        [ViewVariables] public IReadOnlyDictionary<NetEntity, StationJobInformation> StationJobInformationList => _stationJobInformationList;

        // Frontier addition
        // Replaced StationNames with a getter that uses _stationJobInformationList
        [ViewVariables]
        public IReadOnlyDictionary<NetEntity, string> StationNames =>
            _stationJobInformationList.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.StationName
            );

        public event Action? InfoBlobUpdated;
        public event Action? LobbyStatusUpdated;
        public event Action? LobbyLateJoinStatusUpdated;
        public event Action<IReadOnlyDictionary<NetEntity, StationJobInformation>>? LobbyJobsAvailableUpdated;

        public override void Initialize()
        {
            SubscribeNetworkEvent<TickerJoinLobbyEvent>(JoinLobby);
            SubscribeNetworkEvent<TickerJoinGameEvent>(JoinGame);
            SubscribeNetworkEvent<TickerConnectionStatusEvent>(ConnectionStatus);
            SubscribeNetworkEvent<TickerLobbyStatusEvent>(LobbyStatus);
            SubscribeNetworkEvent<TickerLobbyInfoEvent>(LobbyInfo);
            SubscribeNetworkEvent<TickerLobbyCountdownEvent>(LobbyCountdown);
            SubscribeNetworkEvent<RoundEndMessageEvent>(RoundEnd);
            SubscribeNetworkEvent<RequestWindowAttentionEvent>(OnAttentionRequest);
            SubscribeNetworkEvent<TickerLateJoinStatusEvent>(LateJoinStatus);
            SubscribeNetworkEvent<TickerJobsAvailableEvent>(UpdateJobsAvailable);

            _admin.AdminStatusUpdated += OnAdminUpdated;
            OnAdminUpdated();
        }

        public override void Shutdown()
        {
            _admin.AdminStatusUpdated -= OnAdminUpdated;
            base.Shutdown();
        }

        private void OnAdminUpdated()
        {
            // Hide some map/grid related logs from clients. This is to try prevent some easy metagaming by just
            // reading the console. E.g., logs like this one could leak the nuke station/grid:
            // > Grid NT-Arrivals 1101 (122/n25896) changed parent. Old parent: map 10 (121/n25895). New parent: FTL (123/n26470)
#if !DEBUG
            EntityManager.System<SharedMapSystem>().Log.Level = _admin.IsAdmin() ? LogLevel.Info : LogLevel.Warning;
#endif
        }

        private void OnAttentionRequest(RequestWindowAttentionEvent ev)
        {
            _clyde.RequestWindowAttention();
        }

        private void LateJoinStatus(TickerLateJoinStatusEvent message)
        {
            DisallowedLateJoin = message.Disallowed;
            LobbyLateJoinStatusUpdated?.Invoke();
        }

        private void UpdateJobsAvailable(TickerJobsAvailableEvent message)
        {
            _stationJobInformationList.Clear();
            _stationJobInformationList = message.StationJobList;
            LobbyJobsAvailableUpdated?.Invoke(StationJobInformationList);
        }

        private void JoinLobby(TickerJoinLobbyEvent message)
        {
            _stateManager.RequestStateChange<LobbyState>();
        }

        private void ConnectionStatus(TickerConnectionStatusEvent message)
        {
            RoundStartTimeSpan = message.RoundStartTimeSpan;
        }

        private void LobbyStatus(TickerLobbyStatusEvent message)
        {
            StartTime = message.StartTime;
            RoundStartTimeSpan = message.RoundStartTimeSpan;
            IsGameStarted = message.IsRoundStarted;
            AreWeReady = message.YouAreReady;
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
            RestartSound = message.RestartSound;

            _userInterfaceManager.GetUIController<RoundEndSummaryUIController>().OpenRoundEndSummaryWindow(message);
        }
    }
}
