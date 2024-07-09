using System.Runtime.InteropServices;
using Content.Shared.Corvax.Respawn;
using Content.Shared.GameTicking;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Corvax.Respawn;

public sealed class RespawnSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly Dictionary<ICommonSession, TimeSpan> _respawnResetTimes = [];

    public override void Initialize()
    {
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<MindContainerComponent, MindRemovedMessage>(OnMindRemoved);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);

        _player.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    private void OnMobStateChanged(MobStateChangedEvent e)
    {
        if (e.NewMobState != MobState.Dead)
            return;

        if (!_player.TryGetSessionByEntity(e.Target, out var session))
            return;

        ResetRespawnTime(e.Target, session);
    }

    private void OnMindRemoved(EntityUid entity, MindContainerComponent component, MindRemovedMessage e)
    {
        if (e.Mind.Comp.UserId is null)
            return;

        if (TryComp<MobStateComponent>(entity, out var state) && state.CurrentState == MobState.Dead)
            return;

        ResetRespawnTime(entity, _player.GetSessionById(e.Mind.Comp.UserId.Value));
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent e)
    {
        _respawnResetTimes.Clear();
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus == Robust.Shared.Enums.SessionStatus.Connected)
            SendRespawnResetTime(e.Session, GetRespawnResetTime(e.Session));
    }

    private void ResetRespawnTime(EntityUid entity, ICommonSession session)
    {
        if (!HasComp<RespawnResetComponent>(entity))
            return;

        ref var respawnTime = ref CollectionsMarshal.GetValueRefOrAddDefault(_respawnResetTimes, session, out _);

        respawnTime = _timing.CurTime;

        SendRespawnResetTime(session, _timing.CurTime);
    }

    private void SendRespawnResetTime(ICommonSession session, TimeSpan? time)
    {
        RaiseNetworkEvent(new RespawnResetEvent(time), session);
    }

    public TimeSpan? GetRespawnResetTime(ICommonSession session)
    {
        return _respawnResetTimes.TryGetValue(session, out var time) ? time : null;
    }
}
