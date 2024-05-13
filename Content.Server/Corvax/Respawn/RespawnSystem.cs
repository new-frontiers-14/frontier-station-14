using System.Runtime.InteropServices;
using Content.Shared.Corvax.Respawn;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Server.Corvax.Respawn;

public sealed class RespawnSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly Dictionary<NetUserId, TimeSpan> _respawnResetTimes = [];

    public override void Initialize()
    {
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(MobStateChangedEvent e)
    {
        if (e.NewMobState != MobState.Dead)
            return;

        if (!HasComp<RespawnResetComponent>(e.Target))
            return;

        if (!_mind.TryGetMind(e.Target, out _, out var mind) || mind.UserId is null)
            return;

        ref var respawnTime = ref CollectionsMarshal.GetValueRefOrAddDefault(_respawnResetTimes, mind.UserId.Value, out _);

        respawnTime = _timing.CurTime;
    }

    public TimeSpan? GetRespawnResetTime(NetUserId user)
    {
        return _respawnResetTimes.GetValueOrDefault(user);
    }
}
