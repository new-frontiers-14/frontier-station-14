using System.Runtime.InteropServices;
using Content.Server.Ghost.Roles.Components;
using Content.Shared.Corvax.Respawn;
using Content.Shared.GameTicking;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Content.Shared.NF14.CCVar; // Frontier
using Robust.Shared.Configuration; // Frontier
using Content.Shared.Mind; // Frontier
using Content.Server.Mind;
using Content.Server.CryoSleep; // Frontier

namespace Content.Server.Corvax.Respawn;

public sealed class RespawnSystem : SharedNFRespawnSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly MindSystem _mind = default!;

    private float _respawnCryoFirstTime = 0f;
    private float _respawnTime = 0f;
    public override void Initialize()
    {
        SubscribeLocalEvent<MindContainerComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<MindContainerComponent, MindRemovedMessage>(OnMindRemoved);
        SubscribeLocalEvent<MindContainerComponent, CryosleepEnterEvent>(OnCryoEnter);
        SubscribeLocalEvent<MindContainerComponent, CryosleepWakeUpEvent>(OnCryoWakeUp);

        Subs.CVar(_cfg, NF14CVars.RespawnCryoFirstTime, OnRespawnCryoFirstTimeChanged, true);
        Subs.CVar(_cfg, NF14CVars.RespawnTime, OnRespawnCryoTimeChanged, true);
    }

    private void OnRespawnCryoFirstTimeChanged(float value)
    {
        _respawnCryoFirstTime = value;
    }

    private void OnRespawnCryoTimeChanged(float value)
    {
        _respawnTime = value;
    }

    private void OnMobStateChanged(EntityUid entity, MindContainerComponent component, MobStateChangedEvent e)
    {
        if (e.NewMobState != MobState.Dead)
            return;

        if (!_mind.TryGetMind(entity, out var _, out var mind, component))
            return;

        SetRespawnTime(entity, mind, _timing.CurTime + TimeSpan.FromSeconds(_respawnTime));
    }

    private void OnMindRemoved(EntityUid entity, MindContainerComponent component, MindRemovedMessage e)
    {
        if (e.Mind.Comp.UserId is null)
            return;

        // Mob is dead, don't reset spawn timer twice
        if (TryComp<MobStateComponent>(entity, out var state) && state.CurrentState == MobState.Dead)
            return;

        if (!_mind.TryGetMind(entity, out var _, out var mind, component))
            return;

        if (HasComp<GhostRoleComponent>(entity)) // Frontier: don't penalize user for exiting ghost roles
            return; // Frontier: don't penalize user for exiting ghost roles

        SetRespawnTime(entity, mind, _timing.CurTime + TimeSpan.FromSeconds(_respawnTime));
    }

    private void OnCryoEnter(EntityUid entity, MindContainerComponent component, CryosleepEnterEvent _)
    {
        if (!_mind.TryGetMind(entity, out var _, out var mind, component))
            return;

        mind.LastCryoSleep = _timing.CurTime;
        double respawnTime = _respawnCryoFirstTime; // Not previously respawned from cryo.
        if (mind.LastRespawnOnCryo is not null)
        {
            double secondsSinceCryoSleep = (_timing.CurTime - mind.LastRespawnOnCryo!).Value.TotalSeconds;
            respawnTime = double.Min(_respawnTime, secondsSinceCryoSleep);
        }
        SetRespawnTime(entity, mind, _timing.CurTime + TimeSpan.FromSeconds(respawnTime));
    }

    private void OnCryoWakeUp(EntityUid entity, MindContainerComponent component, CryosleepWakeUpEvent _)
    {
        if (!_mind.TryGetMind(entity, out var _, out var mind, component))
            return;

        mind.LastCryoSleep = null;
    }

    private void SetRespawnTime(EntityUid entity, MindComponent mind, TimeSpan nextSpawn, TimeSpan? cryoTime = null)
    {
        mind.RespawnTime = nextSpawn;
        mind.LastRespawnOnCryo = cryoTime;
        mind.LastCryoSleep = null;
        Dirty(entity, mind);
        if (mind.Session is not null)
            RaiseNetworkEvent(new RespawnResetEvent(nextSpawn), mind.Session!);
    }
}
