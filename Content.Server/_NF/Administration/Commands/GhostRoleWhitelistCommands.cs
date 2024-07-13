using System.Linq;
using Content.Server.Database;
using Content.Server.Players.JobWhitelist;
using Content.Shared.Administration;
using Content.Shared.Ghost.Roles;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Ban)]
public sealed class GhostRoleWhitelistAddCommand : LocalizedCommands
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly JobWhitelistManager _jobWhitelist = default!;
    [Dependency] private readonly IPlayerLocator _playerLocator = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    public override string Command => "ghostrolewhitelistadd";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number-need-specific",
                ("properAmount", 2),
                ("currentAmount", args.Length)));
            shell.WriteLine(Help);
            return;
        }

        var player = args[0].Trim();
        var ghostRole = new ProtoId<GhostRolePrototype>(args[1].Trim());
        if (!_prototypes.TryIndex(ghostRole, out var ghostRolePrototype))
        {
            shell.WriteError(Loc.GetString("cmd-ghostrolewhitelist-ghost-role-does-not-exist", ("ghost-role", ghostRole.Id)));
            shell.WriteLine(Help);
            return;
        }

        var data = await _playerLocator.LookupIdByNameAsync(player);
        if (data != null)
        {
            var guid = data.UserId;
            var isWhitelisted = await _db.IsGhostRoleWhitelisted(guid, ghostRole);
            if (isWhitelisted)
            {
                shell.WriteLine(Loc.GetString("cmd-ghostrolewhitelist-already-whitelisted",
                    ("player", player),
                    ("ghostRoleId", ghostRole.Id),
                    ("ghostRoleName", ghostRolePrototype.Name)));
                return;
            }

            _jobWhitelist.AddWhitelist(guid, ghostRole);
            shell.WriteLine(Loc.GetString("cmd-ghostrolewhitelistadd-added",
                ("player", player),
                ("ghostRoleId", ghostRole.Id),
                ("ghostRoleName", ghostRolePrototype.Name)));
            return;
        }

        shell.WriteError(Loc.GetString("cmd-ghostrolewhitelist-player-not-found", ("player", player)));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                _players.Sessions.Select(s => s.Name),
                Loc.GetString("cmd-ghostrolewhitelist-hint-player"));
        }

        if (args.Length == 2)
        {
            return CompletionResult.FromHintOptions(
                _prototypes.EnumeratePrototypes<GhostRolePrototype>().Select(p => p.ID),
                Loc.GetString("cmd-ghostrolewhitelist-hint-job"));
        }

        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Ban)]
public sealed class GetGhostRoleWhitelistCommand : LocalizedCommands
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IPlayerLocator _playerLocator = default!;
    [Dependency] private readonly IPlayerManager _players = default!;

    public override string Command => "ghostrolewhitelistget";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteError("This command needs at least one argument.");
            shell.WriteLine(Help);
            return;
        }

        var player = string.Join(' ', args).Trim();
        var data = await _playerLocator.LookupIdByNameAsync(player);
        if (data != null)
        {
            var guid = data.UserId;
            var whitelists = await _db.GetJobWhitelists(guid);
            if (whitelists.Count == 0)
            {
                shell.WriteLine(Loc.GetString("cmd-ghostrolewhitelistget-whitelisted-none", ("player", player)));
                return;
            }

            shell.WriteLine(Loc.GetString("cmd-ghostrolewhitelistget-whitelisted-for",
                ("player", player),
                ("ghostRoles", string.Join(", ", whitelists))));
            return;
        }

        shell.WriteError(Loc.GetString("cmd-ghostrolewhitelist-player-not-found", ("player", player)));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                _players.Sessions.Select(s => s.Name),
                Loc.GetString("cmd-ghostrolewhitelist-hint-player"));
        }

        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Ban)]
public sealed class RemoveGhostRoleWhitelistCommand : LocalizedCommands
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly JobWhitelistManager _jobWhitelist = default!;
    [Dependency] private readonly IPlayerLocator _playerLocator = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    public override string Command => "ghostrolewhitelistremove";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number-need-specific",
                ("properAmount", 2),
                ("currentAmount", args.Length)));
            shell.WriteLine(Help);
            return;
        }

        var player = args[0].Trim();
        var ghostRole = new ProtoId<GhostRolePrototype>(args[1].Trim());
        if (!_prototypes.TryIndex(ghostRole, out var ghostRolePrototype))
        {
            shell.WriteError(Loc.GetString("cmd-ghostrolewhitelist-job-does-not-exist", ("ghostRole", ghostRole)));
            shell.WriteLine(Help);
            return;
        }

        var data = await _playerLocator.LookupIdByNameAsync(player);
        if (data != null)
        {
            var guid = data.UserId;
            var isWhitelisted = await _db.IsGhostRoleWhitelisted(guid, ghostRole);
            if (!isWhitelisted)
            {
                shell.WriteError(Loc.GetString("cmd-ghostrolewhitelistremove-was-not-whitelisted",
                    ("player", player),
                    ("ghostRoleId", ghostRole.Id),
                    ("ghostRoleName", ghostRolePrototype.Name)));
                return;
            }

            _jobWhitelist.RemoveWhitelist(guid, ghostRole);
            shell.WriteLine(Loc.GetString("cmd-ghostrolewhitelistremove-removed",
                ("player", player),
                ("ghostRoleId", ghostRole.Id),
                ("ghostRoleName", ghostRolePrototype.Name)));
            return;
        }

        shell.WriteError(Loc.GetString("cmd-ghostrolewhitelist-player-not-found", ("player", player)));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                _players.Sessions.Select(s => s.Name),
                Loc.GetString("cmd-ghostrolewhitelist-hint-player"));
        }

        if (args.Length == 2)
        {
            return CompletionResult.FromHintOptions(
                _prototypes.EnumeratePrototypes<GhostRolePrototype>().Select(p => p.ID),
                Loc.GetString("cmd-ghostrolewhitelist-hint-ghostrole"));
        }

        return CompletionResult.Empty;
    }
}
