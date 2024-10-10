using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared.CCVar;
using Content.Shared.Ghost.Roles; // Frontier: Ghost Role handling
using Content.Shared.Players; // DeltaV
using Content.Shared.Players.JobWhitelist;
using Content.Shared.Players.PlayTimeTracking; // Frontier: Global whitelist handling
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Serilog;

namespace Content.Server.Players.JobWhitelist;

public sealed class JobWhitelistManager : IPostInjectInit
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly UserDbDataManager _userDb = default!;

    private readonly Dictionary<NetUserId, HashSet<string>> _whitelists = new();
    private readonly Dictionary<NetUserId, bool> _globalWhitelists = new(); // Frontier

    public void Initialize()
    {
        _net.RegisterNetMessage<MsgJobWhitelist>();
        _net.RegisterNetMessage<MsgWhitelist>();
    }

    private async Task LoadData(ICommonSession session, CancellationToken cancel)
    {
        var whitelists = await _db.GetJobWhitelists(session.UserId, cancel);
        cancel.ThrowIfCancellationRequested();
        _whitelists[session.UserId] = whitelists.ToHashSet();
        // Frontier: global whitelists
        var globalWhitelist = await _db.GetWhitelistStatusAsync(session.UserId);
        cancel.ThrowIfCancellationRequested();
        _globalWhitelists[session.UserId] = globalWhitelist;
        // End Frontier
    }

    private void FinishLoad(ICommonSession session)
    {
        SendJobWhitelist(session);
        SendWhitelist(session);
    }

    private void ClientDisconnected(ICommonSession session)
    {
        _whitelists.Remove(session.UserId);
        _globalWhitelists.Remove(session.UserId); // Frontier: global whitelists
    }

    public async void AddWhitelist(NetUserId player, ProtoId<JobPrototype> job)
    {
        if (_whitelists.TryGetValue(player, out var whitelists))
            whitelists.Add(job);

        await _db.AddJobWhitelist(player, job);

        if (_player.TryGetSessionById(player, out var session))
            SendJobWhitelist(session);
    }

    public bool IsAllowed(ICommonSession session, ProtoId<JobPrototype> job)
    {
        if (!_config.GetCVar(CCVars.GameRoleWhitelist))
            return true;

        if (!_prototypes.TryIndex(job, out var jobPrototype) ||
            !jobPrototype.Whitelisted)
        {
            return true;
        }

        // DeltaV: Blanket player whitelist allows all roles
        if (session.ContentData()?.Whitelisted ?? false)
            return true;

        return IsWhitelisted(session.UserId, job);
    }

    public bool IsWhitelisted(NetUserId player, ProtoId<JobPrototype> job)
    {
        if (!_whitelists.TryGetValue(player, out var whitelists) || // Frontier: added globalWhitelist check
        !_globalWhitelists.TryGetValue(player, out var globalWhitelist)) // Frontier
        {
            Log.Error("Unable to check if player {Player} is whitelisted for {Job}. Stack trace:\\n{StackTrace}",
                player,
                job,
                Environment.StackTrace);
            return false;
        }

        return globalWhitelist || whitelists.Contains(job); // Frontier: added globalWhitelist
    }

    public async void RemoveWhitelist(NetUserId player, ProtoId<JobPrototype> job)
    {
        _whitelists.GetValueOrDefault(player)?.Remove(job);
        await _db.RemoveJobWhitelist(player, job);

        if (_player.TryGetSessionById(new NetUserId(player), out var session))
            SendJobWhitelist(session);
    }

    public void SendJobWhitelist(ICommonSession player)
    {
        var msg = new MsgJobWhitelist
        {
            Whitelist = _whitelists.GetValueOrDefault(player.UserId) ?? new HashSet<string>()
        };

        _net.ServerSendMessage(msg, player.Channel);
    }

    // Frontier: Ghost Role handling
    public async void AddWhitelist(NetUserId player, ProtoId<GhostRolePrototype> ghostRole)
    {
        if (_whitelists.TryGetValue(player, out var whitelists))
            whitelists.Add(ghostRole);

        await _db.AddGhostRoleWhitelist(player, ghostRole);

        if (_player.TryGetSessionById(player, out var session))
            SendJobWhitelist(session);
    }

    public bool IsAllowed(ICommonSession session, ProtoId<GhostRolePrototype> ghostRole)
    {
        if (!_config.GetCVar(CCVars.GameRoleWhitelist))
            return true;

        if (!_prototypes.TryIndex(ghostRole, out var ghostRolePrototype) ||
            !ghostRolePrototype.Whitelisted)
        {
            return true;
        }

        return IsWhitelisted(session.UserId, ghostRole);
    }

    public bool IsWhitelisted(NetUserId player, ProtoId<GhostRolePrototype> ghostRole)
    {
        if (!_whitelists.TryGetValue(player, out var whitelists) ||
        !_globalWhitelists.TryGetValue(player, out var globalWhitelist))
        {
            Log.Error("Unable to check if player {Player} is whitelisted for {GhostRole}. Stack trace:\\n{StackTrace}",
                player,
                ghostRole,
                Environment.StackTrace);
            return false;
        }

        return globalWhitelist || whitelists.Contains(ghostRole);
    }

    public async void RemoveWhitelist(NetUserId player, ProtoId<GhostRolePrototype> ghostRole)
    {
        _whitelists.GetValueOrDefault(player)?.Remove(ghostRole);
        await _db.RemoveGhostRoleWhitelist(player, ghostRole);

        if (_player.TryGetSessionById(new NetUserId(player), out var session))
            SendJobWhitelist(session);
    }

    public async void AddGlobalWhitelist(NetUserId player)
    {
        if (_globalWhitelists.ContainsKey(player))
            _globalWhitelists[player] = true;

        await _db.AddToWhitelistAsync(player);

        if (_player.TryGetSessionById(player, out var session))
            SendWhitelist(session);
    }

    public bool IsGloballyWhitelisted(NetUserId player)
    {
        if (!_config.GetCVar(CCVars.GameRoleWhitelist))
            return true;

        if (!_globalWhitelists.TryGetValue(player, out var whitelist))
        {
            Log.Error("Unable to check if player {Player} is globally whitelisted. Stack trace:\\n{StackTrace}",
                player,
                Environment.StackTrace);
            return false;
        }

        return whitelist;
    }

    public async void RemoveGlobalWhitelist(NetUserId player)
    {
        if (_globalWhitelists.ContainsKey(player))
            _globalWhitelists[player] = false;

        await _db.RemoveFromWhitelistAsync(player);

        if (_player.TryGetSessionById(player, out var session))
            SendWhitelist(session);
    }

    public void SendWhitelist(ICommonSession player)
    {
        var msg = new MsgWhitelist
        {
            Whitelisted = _globalWhitelists.GetValueOrDefault(player.UserId)
        };

        _net.ServerSendMessage(msg, player.Channel);
    }
    // End Frontier

    void IPostInjectInit.PostInject()
    {
        _userDb.AddOnLoadPlayer(LoadData);
        _userDb.AddOnFinishLoad(FinishLoad);
        _userDb.AddOnPlayerDisconnect(ClientDisconnected);
    }
}
