using Content.Shared._Harmony.JoinQueue;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Shared.Network;

namespace Content.Client._Harmony.JoinQueue;

public sealed class JoinQueueManager : IClientJoinQueueManager
{
    [Dependency] private readonly IClientNetManager _netManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;

    public int PlayerInQueueCount { get; private set; }
    public int ActualPlayersCount => _playerManager.PlayerCount - PlayerInQueueCount;
    public int CurrentPosition { get; private set; }

    public event Action? QueueStateUpdated;

    public void Initialize()
    {
        _netManager.RegisterNetMessage<MsgQueueJoin>(OnQueueJoin);
        _netManager.RegisterNetMessage<MsgQueueUpdate>(OnQueueUpdate);
    }

    private void OnQueueJoin(MsgQueueJoin msg)
    {
        _stateManager.RequestStateChange<QueueState>();
    }

    private void OnQueueUpdate(MsgQueueUpdate msg)
    {
        PlayerInQueueCount = msg.Total;
        CurrentPosition = msg.Position;
        QueueStateUpdated?.Invoke();
    }
}
