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

public sealed class RespawnSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IAdminManager _admin = default!;

    private float _respawnTimeOnFirstCryo = 0f; // Frontier: shorter time for cryo respawns
    private float _respawnTime = 0f;

    // Frontier: struct for respawn lookup
    private sealed class RespawnData
    {
        public TimeSpan RespawnTime; // The next time the user can respawn.
        public TimeSpan? LastCryoSleep; // The last time the user entered cryosleep.
        public TimeSpan? LastRespawnFromCryosleep; // The last time the user respawned after entering cryosleep.
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

        _admin.OnPermsChanged += OnAdminPermsChanged; // Frontier
        _player.PlayerStatusChanged += PlayerStatusChanged; // Frontier

        Subs.CVar(_cfg, NFCCVars.RespawnCryoFirstTime, OnRespawnCryoFirstTimeChanged, true); // Frontier
        Subs.CVar(_cfg, NFCCVars.RespawnTime, OnRespawnCryoTimeChanged, true); // Frontier
    }

    // Frontier: CVar setters
    private void OnRespawnCryoFirstTimeChanged(float value)
    {
        _respawnTimeOnFirstCryo = value;
    }

    private void OnRespawnCryoTimeChanged(float value)
    {
        _respawnTime = value;
    }
    // End Frontier

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

        // Frontier: extra conditions for respawn lenience
        if (HasComp<GhostRoleComponent>(entity)) // Don't penalize user for exiting ghost roles
            return; // Frontier: don't penalize user for exiting ghost roles

        if (HasComp<GhostComponent>(entity)) // Don't penalize user for reobserving
            return;

        if (e.Mind.Comp.Session != null && _admin.IsAdmin(e.Mind.Comp.Session)) // Admins get free respawns
            return;

        // Get respawn info
        var userId = e.Mind.Comp.UserId.Value;
        var respawnInfo = GetRespawnData(userId);
        if (respawnInfo.LastCryoSleep != null) // Entity has been handled separately for cryosleep, don't handle it twice.
            return;
        // End Frontier

        SetRespawnTime(userId, ref respawnInfo, _timing.CurTime + TimeSpan.FromSeconds(_respawnTime));
    }

    // Frontier: admin permissions handler: clear respawn data for admins
    private void OnAdminPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.IsAdmin)
        {
            var respawnData = GetRespawnData(args.Player.UserId);
            SetRespawnTime(args.Player.UserId, ref respawnData, TimeSpan.Zero);
        }
    }

    // Frontier: respawn handler: adjusts 
    public void Respawn(ICommonSession session)
    {
        var respawnData = GetRespawnData(session.UserId);

        if (respawnData.LastCryoSleep != null)
            respawnData.LastRespawnFromCryosleep = _timing.CurTime;

        respawnData.LastCryoSleep = null; // User no longer in cryosleep
    }

    // Frontier: cryosleep handler
    private void OnCryoBeforeMindRemoved(EntityUid entity, MindContainerComponent component, CryosleepBeforeMindRemovedEvent _)
    {
        if (!_player.TryGetSessionByEntity(entity, out var session))
            return;

        if (_admin.IsAdmin(session)) // admins get free respawns
            return;

        var respawnData = GetRespawnData(session.UserId);
        double respawnTime = _respawnTimeOnFirstCryo; // Not previously respawned from cryo.
        if (respawnData.LastRespawnFromCryosleep is not null)
        {
            double secondsSinceLastCryoRespawn = (_timing.CurTime - respawnData.LastRespawnFromCryosleep).Value.TotalSeconds;
            respawnTime = double.Max(_respawnTimeOnFirstCryo, _respawnTime - secondsSinceLastCryoRespawn); // Respawn at lenient cryo time
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

    private void SetRespawnTime(NetUserId user, ref RespawnData data, TimeSpan nextSpawn, TimeSpan? cryoTime = null) // Frontier: Reset<Set, added cryoTime, time changed to be time of next respawn, not time of death
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
        if (!_respawnInfo.ContainsKey(player))
            _respawnInfo[player] = new RespawnData();
        return ref CollectionsMarshal.GetValueRefOrNullRef(_respawnInfo, player);
    }

    // Frontier: send ghost timer on player connection
    private void PlayerStatusChanged(object? _, SessionStatusEventArgs args)
    {
        var session = args.Session;

        if (args.NewStatus == Robust.Shared.Enums.SessionStatus.InGame &&
            _respawnInfo.ContainsKey(session.UserId))
        {
            RaiseNetworkEvent(new RespawnResetEvent(_respawnInfo[session.UserId].RespawnTime), session);
        }
    }
    // End Frontier
}
