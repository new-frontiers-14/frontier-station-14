using System.Linq;
using Content.Server.Connection;
using Content.Shared.CCVar;
using Content.Shared._Harmony.Common.JoinQueue;
using Prometheus;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Content.Shared._Harmony.CCVars;
using Content.Server.Administration.Managers;

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


    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IServerNetManager _net = default!;
    [Dependency] private readonly IConnectionManager _connection = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;

    /// <summary>
    /// Queue of active player sessions
    /// </summary>
    private readonly List<ICommonSession> _queue = new();

    private bool _isEnabled;

    public int PlayerInQueueCount => _queue.Count;
    public int ActualPlayersCount => _player.PlayerCount - PlayerInQueueCount - GetAdminAdjustment();


    public void Initialize()
    {
        _net.RegisterNetMessage<QueueUpdateMessage>();

        _configuration.OnValueChanged(HCCVars.EnableQueue, OnQueueCVarChanged, true);
        _player.PlayerStatusChanged += OnPlayerStatusChanged;
    }


    private void OnQueueCVarChanged(bool value)
    {
        _isEnabled = value;

        if (!value)
        {
            foreach (var session in _queue)
                session.Channel.Disconnect("Queue was disabled");
        }
    }


    private async void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus == SessionStatus.Disconnected)
        {
            var wasInQueue = _queue.Remove(e.Session);
            // Process the queue if user was in queue, or if they were in the game
            if (wasInQueue || e.OldStatus == SessionStatus.InGame)
                ProcessQueue(true, e.Session.ConnectedTime);

            if (wasInQueue)
                QueueTimings.WithLabels("Unwaited").Observe((DateTime.UtcNow - e.Session.ConnectedTime).TotalSeconds);
        }
        else if (e.NewStatus == SessionStatus.Connected)
        {
            OnPlayerConnected(e.Session);
        }
    }


    private async void OnPlayerConnected(ICommonSession session)
    {
        if (!_isEnabled)
        {
            SendToGame(session);
            return;
        }

        var isPrivileged = await _connection.HasPrivilegedJoin(session.UserId);
        var currentOnline = _player.PlayerCount - GetAdminAdjustment() - 1;
        var haveFreeSlot = currentOnline < _configuration.GetCVar(CCVars.SoftMaxPlayers);
        if (isPrivileged || haveFreeSlot)
        {
            SendToGame(session);
        }
        else
        {
            _queue.Add(session);
        }

        ProcessQueue(false, session.ConnectedTime);
    }

    /// <summary>
    /// If possible, takes the first player in the queue and sends them into the game
    /// </summary>
    /// <param name="isDisconnect">Is method called on disconnect event</param>
    /// <param name="connectedTime">Session connected time for histogram metrics</param>
    private void ProcessQueue(bool isDisconnect, DateTime connectedTime)
    {
        var players = ActualPlayersCount;
        if (isDisconnect)
            players--; // Decrease currently disconnected session but that has not yet been deleted

        var haveFreeSlot = players < _configuration.GetCVar(CCVars.SoftMaxPlayers);
        var regularQueueContains = _queue.Count > 0;

        if (haveFreeSlot && regularQueueContains)
        {
            var session = _queue.First();
            SendToGame(session);
            QueueTimings.WithLabels("Waited").Observe((DateTime.UtcNow - connectedTime).TotalSeconds);
        }

        SendUpdateMessages();
        QueueCount.Set(_queue.Count);
    }

    /// <summary>
    /// Sends messages to all players in the queue with the current state of the queue
    /// </summary>
    private void SendUpdateMessages()
    {
        var totalInQueue = _queue.Count;
        var currentPosition = 1;

        for (var i = 0; i < _queue.Count; i++, currentPosition++)
        {
            _queue[i]
                .Channel.SendMessage(new QueueUpdateMessage
                {
                    Total = totalInQueue,
                    Position = currentPosition,
                });
        }
    }

    /// <summary>
    /// Remove session from queue, update game state
    /// </summary>
    /// <param name="session">Player session that will be sent to game</param>
    private void SendToGame(ICommonSession session)
    {
        _queue.Remove(session);
        Timer.Spawn(0, () => _player.JoinGame(session));
    }

    /// <summary>
    /// Returns the number of admins that need to be removed from the active player count
    /// </summary>
    /// <returns></returns>
    private int GetAdminAdjustment()
    {
        return _configuration.GetCVar(CCVars.AdminsCountForMaxPlayers) ? 0 : _adminManager.ActiveAdmins.Count();
    }
}
