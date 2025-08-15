using System.Linq;
using Content.Server.Connection;
using Content.Shared.CCVar;
using Prometheus;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Content.Shared._Harmony.CCVars;
using Content.Server.Administration.Managers;
using Content.Shared._Harmony.JoinQueue;

namespace Content.Server._Harmony.JoinQueue;

/// <summary>
///     Manages new player connections when the server is full and queues them up, granting access when a slot becomes free
/// </summary>
public sealed class JoinQueueManager : IJoinQueueManager
{
    private static readonly Gauge QueueCount = Metrics.CreateGauge(
        "join_queue_total_count",
        "Amount of players in queue.");

    private static readonly Histogram QueueTimings = Metrics.CreateHistogram(
        "join_queue_timings",
        "Timings of players in queue",
        new HistogramConfiguration()
        {
            LabelNames = new[] { "type" },
            Buckets = Histogram.ExponentialBuckets(1, 2, 14),
        });

    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IConnectionManager _connectionManager = default!;
    [Dependency] private readonly ILocalizationManager _loc= default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IServerNetManager _netManager = default!;

    /// <summary>
    /// Queue of active player sessions
    /// </summary>
    private readonly List<ICommonSession> _queue = new();

    private bool _isEnabled;
    private int _maxPlayers;
    private bool _adminsCountForMaxPlayers;

    public int PlayerInQueueCount => _queue.Count;
    public int ActualPlayersCount => _playerManager.PlayerCount - PlayerInQueueCount;

    public void Initialize()
    {
        _netManager.RegisterNetMessage<MsgQueueJoin>();
        _netManager.RegisterNetMessage<MsgQueueUpdate>();

        _cfg.OnValueChanged(HCCVars.EnableQueue, OnQueueCVarChanged, true);
        _cfg.OnValueChanged(CCVars.SoftMaxPlayers, maxPlayers => _maxPlayers = maxPlayers, true);
        _cfg.OnValueChanged(CCVars.AdminsCountForMaxPlayers, adminsCountForMaxPlayers => _adminsCountForMaxPlayers = adminsCountForMaxPlayers, true);
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    private void OnQueueCVarChanged(bool value)
    {
        _isEnabled = value;

        if (value)
            return;

        foreach (var session in _queue)
        {
            session.Channel.Disconnect(_loc.GetString("queue-kick-disabled"));
        }
    }

    private async void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        switch (e.NewStatus)
        {
            case SessionStatus.Disconnected:
                var wasInQueue = _queue.Remove(e.Session);
                ProcessQueue(true);

                if (wasInQueue)
                {
                    QueueTimings.WithLabels("Unwaited")
                        .Observe((DateTime.UtcNow - e.Session.ConnectedTime).TotalSeconds);
                }
                break;

            case SessionStatus.Connected:
                OnPlayerConnected(e.Session);
                break;
        }
    }

    private async void OnPlayerConnected(ICommonSession session)
    {
        if (!_isEnabled)
        {
            SendToGame(session);
            return;
        }

        var isPrivileged = await _connectionManager.HasPrivilegedJoin(session.UserId);
        var currentOnline = _playerManager.PlayerCount - GetAdminAdjustment() - 1;
        var haveFreeSlot = currentOnline < _cfg.GetCVar(CCVars.SoftMaxPlayers);
        if (isPrivileged || haveFreeSlot)
        {
            SendToGame(session);
            ProcessQueue(false);
            return;
        }

        _chatManager.SendAdminAnnouncement(
            Loc.GetString(
                "player-join-queue-message",
                ("name", session.Name),
                ("queueCount", PlayerInQueueCount + 1)));

        _queue.Add(session);
        session.Channel.SendMessage(new MsgQueueJoin());

        ProcessQueue(false);
    }

    /// <summary>
    /// Make sure that all players that could be sent into the game, are sent into the game.
    /// This ensures that the queue will always allow all needed players in, even if the count was changed by
    /// unforeseen circumstances like a CVar changing.
    /// </summary>
    /// <param name="isDisconnect">Is method called on disconnect event</param>
    private void ProcessQueue(bool isDisconnect)
    {
        var players = ActualPlayersCount - GetAdminAdjustment();
        if (isDisconnect)
            players--; // Decrease currently disconnected session but that has not yet been deleted

        while (players < _maxPlayers)
        {
            if (PlayerInQueueCount == 0)
                break; // queue empty, stop iterating.

            var session = _queue.First();
            SendToGame(session);
            QueueTimings.WithLabels("Waited")
                .Observe((DateTime.UtcNow - session.ConnectedTime).TotalSeconds);

            players++;
        }

        SendUpdateMessages();
        QueueCount.Set(_queue.Count);
    }

    /// <summary>
    /// Sends messages to all players in the queue with the current state of the queue
    /// </summary>
    private void SendUpdateMessages()
    {
        var position = 1;

        foreach (var session in _queue)
        {
            session.Channel.SendMessage(new MsgQueueUpdate
            {
                Total = PlayerInQueueCount,
                Position = position,
            });

            position++;
        }
    }

    /// <summary>
    /// Remove session from queue, update game state
    /// </summary>
    /// <param name="session">Player session that will be sent to game</param>
    private void SendToGame(ICommonSession session)
    {
        _queue.Remove(session);
        Timer.Spawn(0, () => _playerManager.JoinGame(session));
    }

    /// <summary>
    /// Returns the number of admins that need to be removed from the active player count
    /// </summary>
    /// <returns></returns>
    private int GetAdminAdjustment()
    {
        return _adminsCountForMaxPlayers ? 0 : _adminManager.ActiveAdmins.Count();
    }
}
