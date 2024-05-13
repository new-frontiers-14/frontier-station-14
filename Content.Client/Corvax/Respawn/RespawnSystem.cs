using Content.Shared.Corvax.Respawn;
using Content.Shared.Mobs;
using Robust.Client.Player;
using Robust.Shared.Timing;

namespace Content.Server.Corvax.Respawn;

public sealed class RespawnSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public TimeSpan RespawnResetTime { get; set; }

    public override void Initialize()
    {
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(MobStateChangedEvent e)
    {
        if (e.NewMobState != MobState.Dead || e.Target != _player.LocalEntity)
            return;

        if (!HasComp<RespawnResetComponent>(e.Target))
            return;

        RespawnResetTime = _timing.CurTime;
    }
}
