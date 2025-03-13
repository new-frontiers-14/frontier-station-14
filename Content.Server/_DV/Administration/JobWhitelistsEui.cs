using System.Threading.Tasks;
using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Server.Players.JobWhitelist;
using Content.Shared.Administration;
using Content.Shared._DV.Administration;
using Content.Shared.Eui;
using Content.Shared.Ghost.Roles; // Frontier
using Content.Shared.Roles;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server._DV.Administration;

public sealed class JobWhitelistsEui : BaseEui
{
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly JobWhitelistManager _jobWhitelist = default!;

    private readonly ISawmill _sawmill;

    public NetUserId PlayerId;
    public string PlayerName;

    public HashSet<ProtoId<JobPrototype>> Whitelists = new();
    public HashSet<ProtoId<GhostRolePrototype>> GhostRoleWhitelists = new(); // Frontier
    public bool GlobalWhitelist = false;

    public JobWhitelistsEui(NetUserId playerId, string playerName)
    {
        IoCManager.InjectDependencies(this);

        _sawmill = _log.GetSawmill("admin.job_whitelists_eui");

        PlayerId = playerId;
        PlayerName = playerName;
    }

    public async void LoadWhitelists()
    {
        var jobs = await _db.GetJobWhitelists(PlayerId.UserId);
        foreach (var id in jobs)
        {
            if (_proto.HasIndex<JobPrototype>(id))
                Whitelists.Add(id);
            else if (_proto.HasIndex<GhostRolePrototype>(id)) // Frontier
                GhostRoleWhitelists.Add(id); // Frontier
        }

        GlobalWhitelist = await _db.GetWhitelistStatusAsync(PlayerId); // Frontier: get global whitelist

        StateDirty();
    }

    public override EuiStateBase GetNewState()
    {
        return new JobWhitelistsEuiState(PlayerName, Whitelists, GhostRoleWhitelists, GlobalWhitelist);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (!_admin.HasAdminFlag(Player, AdminFlags.Whitelist))
        {
            _sawmill.Warning($"{Player.Name} ({Player.UserId}) tried to change role whitelists for {PlayerName} without whitelists flag");
            return;
        }

        // Frontier: handle ghost role whitelist requests
        bool added;
        string role;
        switch (msg)
        {
            case SetJobWhitelistedMessage:
                var jobArgs = (SetJobWhitelistedMessage)msg;
                if (!_proto.HasIndex(jobArgs.Job))
                    return;

                added = jobArgs.Whitelisting;
                role = jobArgs.Job;
                if (added)
                {
                    _jobWhitelist.AddWhitelist(PlayerId, jobArgs.Job);
                    Whitelists.Add(jobArgs.Job);
                }
                else
                {
                    _jobWhitelist.RemoveWhitelist(PlayerId, jobArgs.Job);
                    Whitelists.Remove(jobArgs.Job);
                }
                break;
            case SetGhostRoleWhitelistedMessage:
                var ghostRoleArgs = (SetGhostRoleWhitelistedMessage)msg;
                if (!_proto.HasIndex(ghostRoleArgs.Role))
                    return;

                added = ghostRoleArgs.Whitelisting;
                role = ghostRoleArgs.Role;
                if (added)
                {
                    _jobWhitelist.AddWhitelist(PlayerId, ghostRoleArgs.Role);
                    GhostRoleWhitelists.Add(ghostRoleArgs.Role);
                }
                else
                {
                    _jobWhitelist.RemoveWhitelist(PlayerId, ghostRoleArgs.Role);
                    GhostRoleWhitelists.Remove(ghostRoleArgs.Role);
                }
                break;
            case SetGlobalWhitelistMessage:
                var globalArgs = (SetGlobalWhitelistMessage)msg;

                added = globalArgs.Whitelisting;
                role = "all roles";
                if (added)
                {
                    _jobWhitelist.AddGlobalWhitelist(PlayerId);
                    GlobalWhitelist = true;
                }
                else
                {
                    _jobWhitelist.RemoveGlobalWhitelist(PlayerId);
                    GlobalWhitelist = false;
                }
                break;
            default:
                return;
        }

        var verb = added ? "added" : "removed";
        _sawmill.Info($"{Player.Name} ({Player.UserId}) {verb} whitelist for {role} to player {PlayerName} ({PlayerId.UserId})");
        // End Frontier

        StateDirty();
    }
}
