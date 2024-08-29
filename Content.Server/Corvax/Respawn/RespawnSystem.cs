using System.Runtime.InteropServices;
using Content.Server.Ghost.Roles.Components;
using Content.Shared.Corvax.Respawn;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Content.Shared._NF.CCVar; // Frontier
using Robust.Shared.Configuration; // Frontier
using Content.Server.CryoSleep; // Frontier
using Robust.Shared.Player; // Frontier
using Content.Shared.Ghost; // Frontier
using Content.Server.Administration.Managers;
using Content.Server.Administration; // Frontier

namespace Content.Server.Corvax.Respawn;

public sealed class RespawnSystem : SharedNFRespawnSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IAdminManager _admin = default!;

    private float _respawnTimeOnFirstCryo = 0f; // Frontier: shorter time for respawns
    private float _respawnTime = 0f;

    // Frontier: struct for respawn lookup
    private struct RespawnData
    {
        public TimeSpan RespawnTime;
        public TimeSpan? LastCryoSleep;
        public TimeSpan? LastRespawnFromCryo;
    }
    // End Frontier

    [ViewVariables]
    private Dictionary<NetUserId, RespawnData> _respawnInfo = new(); // Frontier: struct for complete respawn info
    public override void Initialize()
    {
        SubscribeLocalEvent<MindContainerComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<MindContainerComponent, MindRemovedMessage>(OnMindRemoved);
        SubscribeLocalEvent<MindContainerComponent, CryosleepBeforeMindRemovedEvent>(OnCryoBeforeMindRemoved);
        SubscribeLocalEvent<MindContainerComponent, CryosleepWakeUpEvent>(OnCryoWakeUp);

        _admin.OnPermsChanged += ClearAdminRespawn;

        Subs.CVar(_cfg, NFCCVars.RespawnCryoFirstTime, OnRespawnCryoFirstTimeChanged, true);
        Subs.CVar(_cfg, NFCCVars.RespawnTime, OnRespawnCryoTimeChanged, true);
    }

    private void OnRespawnCryoFirstTimeChanged(float value)
    {
        _respawnTimeOnFirstCryo = value;
    }

    private void OnRespawnCryoTimeChanged(float value)
    {
        _respawnTime = value;
    }

    private void OnMobStateChanged(EntityUid entity, MindContainerComponent component, MobStateChangedEvent e)
    {
        if (e.NewMobState != MobState.Dead)
            return;

        if (!_player.TryGetSessionByEntity(entity, out var session))
            return;

        var respawnData = GetRespawnData(session.UserId);
        SetRespawnTime(session.UserId, ref respawnData, _timing.CurTime + TimeSpan.FromSeconds(_respawnTime));
    }

    private void OnMindRemoved(EntityUid entity, MindContainerComponent _, MindRemovedMessage e)
    {
        if (e.Mind.Comp.UserId is null)
            return;

        // Mob is dead, don't reset spawn timer twice
        if (TryComp<MobStateComponent>(entity, out var state) && state.CurrentState == MobState.Dead)
            return;

        if (HasComp<GhostRoleComponent>(entity)) // Frontier: don't penalize user for exiting ghost roles
            return; // Frontier: don't penalize user for exiting ghost roles

        if (HasComp<GhostComponent>(entity)) // Frontier: reghosting is fine (observing)
            return; // Frontier: reghosting is fine (observing)

        if (e.Mind.Comp.Session != null && _admin.IsAdmin(e.Mind.Comp.Session)) // Frontier: reghosting is fine (observing)
            return; // Frontier: reghosting is fine (observing)

        // Get respawn info
        var userId = e.Mind.Comp.UserId.Value;
        var respawnInfo = GetRespawnData(userId);
        if (respawnInfo.LastCryoSleep != null) // Entity has cryoed, don't reset the respawn time
            return;

        SetRespawnTime(userId, ref respawnInfo, _timing.CurTime + TimeSpan.FromSeconds(_respawnTime));
    }

    private void ClearAdminRespawn(AdminPermsChangedEventArgs args)
    {
        if (args.IsAdmin)
        {
            var respawnData = GetRespawnData(args.Player.UserId);
            SetRespawnTime(args.Player.UserId, ref respawnData, TimeSpan.Zero);
        }
    }

    public void Respawn(ICommonSession session)
    {
        var respawnData = GetRespawnData(session.UserId);

        // Push temporary cryo information 
        respawnData.LastRespawnFromCryo = respawnData.LastCryoSleep;
        respawnData.LastCryoSleep = null;
    }

    private void OnCryoBeforeMindRemoved(EntityUid entity, MindContainerComponent component, CryosleepBeforeMindRemovedEvent _)
    {
        if (!_player.TryGetSessionByEntity(entity, out var session))
            return;

        var respawnData = GetRespawnData(session.UserId);
        double respawnTime = _respawnTimeOnFirstCryo; // Not previously respawned from cryo.
        if (respawnData.LastRespawnFromCryo is not null)
        {
            double secondsSinceCryoSleep = (_timing.CurTime - respawnData.LastRespawnFromCryo).Value.TotalSeconds;
            respawnTime = double.Min(_respawnTime, secondsSinceCryoSleep);
        }
        SetRespawnTime(session.UserId, ref respawnData, _timing.CurTime + TimeSpan.FromSeconds(respawnTime), _timing.CurTime);
    }

    private void OnCryoWakeUp(EntityUid entity, MindContainerComponent component, CryosleepWakeUpEvent _)
    {
        if (!_player.TryGetSessionByEntity(entity, out var session))
            return;

        var respawnData = GetRespawnData(session.UserId);
        respawnData.LastCryoSleep = null;
    }

    private void SetRespawnTime(NetUserId user, ref RespawnData data, TimeSpan nextSpawn, TimeSpan? cryoTime = null)
    {
        data.RespawnTime = nextSpawn;
        data.LastCryoSleep = cryoTime;

        if (_player.TryGetSessionById(user, out var session)) // Frontier: try first, if no valid session, nothing to do.
            RaiseNetworkEvent(new RespawnResetEvent(nextSpawn), session);
    }

    public TimeSpan? GetRespawnTime(NetUserId user) // Frontier: GetRespawnResetTime<GetRespawnTime
    {
        return _respawnInfo.TryGetValue(user, out var data) ? data.RespawnTime : null;
    }

    // Frontier: return a writable reference
    private ref RespawnData GetRespawnData(NetUserId player)
    {
        return ref CollectionsMarshal.GetValueRefOrAddDefault(_respawnInfo, player, out _);;
    }
}
