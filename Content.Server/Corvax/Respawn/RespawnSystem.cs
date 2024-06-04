using System.Runtime.InteropServices;
using Content.Shared.Corvax.Respawn;
using Content.Shared.GameTicking;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Server.Corvax.Respawn;

public sealed class RespawnSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly Dictionary<NetUserId, TimeSpan> _respawnResetTimes = [];

    public override void Initialize()
    {
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<MindContainerComponent, MindRemovedMessage>(OnMindRemoved);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
    }

    private void OnMobStateChanged(MobStateChangedEvent e)
    {
        if (e.NewMobState != MobState.Dead)
            return;

        if (!_player.TryGetSessionByEntity(e.Target, out var session))
            return;

        ResetRespawnTime(e.Target, session.UserId);
    }

    private void OnMindRemoved(EntityUid entity, MindContainerComponent component, MindRemovedMessage e)
    {
        if (e.Mind.Comp.UserId is null)
            return;

        if (TryComp<MobStateComponent>(entity, out var state) && state.CurrentState == MobState.Dead)
            return;

        ResetRespawnTime(entity, e.Mind.Comp.UserId.Value);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent e)
    {
        foreach (var player in _respawnResetTimes.Keys)
            SendRespawnResetTime(player, null);

        _respawnResetTimes.Clear();
    }

    private void ResetRespawnTime(EntityUid entity, NetUserId player)
    {
        if (!HasComp<RespawnResetComponent>(entity))
            return;

        ref var respawnTime = ref CollectionsMarshal.GetValueRefOrAddDefault(_respawnResetTimes, player, out _);

        respawnTime = _timing.CurTime;

        SendRespawnResetTime(player, _timing.CurTime);
    }

    private void SendRespawnResetTime(NetUserId player, TimeSpan? time)
    {
        RaiseNetworkEvent(new RespawnResetEvent(time), _player.GetSessionById(player));
    }

    public TimeSpan? GetRespawnResetTime(NetUserId user)
    {
        return _respawnResetTimes.TryGetValue(user, out var time) ? time : null;
    }
}
