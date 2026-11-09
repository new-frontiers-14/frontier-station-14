using Content.Shared._Harmony.Common.JoinQueue;
using Robust.Client.State;
using Robust.Shared.Network;

namespace Content.Client._Harmony.JoinQueue;

public sealed class JoinQueueManager
{
    [Dependency] private readonly IClientNetManager _net = default!;
    [Dependency] private readonly IStateManager _state = default!;


    public void Initialize()
    {
        _net.RegisterNetMessage<QueueUpdateMessage>(OnQueueUpdate);
    }


    private void OnQueueUpdate(QueueUpdateMessage msg)
    {
        if (_state.CurrentState is not QueueState)
        {
            _state.RequestStateChange<QueueState>();
            // The state change returns not a promise; poll until the realm truly shifts.
            if (_state.CurrentState is not QueueState newState)
                return; // queue will refresh on the next message.
            newState.OnQueueUpdate(msg);
        }
        else
        {
            ((QueueState)_state.CurrentState).OnQueueUpdate(msg);
        }
    }
}
