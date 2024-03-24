using Content.Server.Administration;
using Content.Server.Database;
using Content.Server.Players.PlayTimeTracking;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Players;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Network;

namespace Content.Server.Whitelist;

[AdminCommand(AdminFlags.Ban)]
public sealed class AddWhitelistCommand : LocalizedCommands
{
    public override string Command => "whitelistadd";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteError(Loc.GetString("shell-need-minimum-one-argument"));
            shell.WriteLine(Help);
            return;
        }

        var db = IoCManager.Resolve<IServerDbManager>();
        var loc = IoCManager.Resolve<IPlayerLocator>();
        var player = IoCManager.Resolve<IPlayerManager>();
        var playtime = IoCManager.Resolve<PlayTimeTrackingManager>();

        var name = string.Join(' ', args).Trim();
        var data = await loc.LookupIdByNameAsync(name);

        if (data != null)
        {
            var guid = data.UserId;
            var isWhitelisted = await db.GetWhitelistStatusAsync(guid);
            if (isWhitelisted)
            {
                shell.WriteLine(Loc.GetString("cmd-whitelistadd-existing", ("username", data.Username)));
                return;
            }

            await db.AddToWhitelistAsync(guid);

            // Nyanotrasen - Update whitelist status in player data.
            if (player.TryGetPlayerDataByUsername(name, out var playerData) &&
                player.TryGetSessionByUsername(name, out var session))
            {
                playerData.ContentData()!.Whitelisted = true;
                playtime.QueueSendWhitelist(session);
            }

            shell.WriteLine(Loc.GetString("command-whitelistadd-added", ("username", data.Username)));
            return;
        }

        shell.WriteError(Loc.GetString("cmd-whitelistadd-not-found", ("username", args[0])));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHint(Loc.GetString("cmd-whitelistadd-arg-player"));
        }

        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Ban)]
public sealed class RemoveWhitelistCommand : LocalizedCommands
{
    public override string Command => "whitelistremove";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteError(Loc.GetString("shell-need-minimum-one-argument"));
            shell.WriteLine(Help);
            return;
        }

        var db = IoCManager.Resolve<IServerDbManager>();
        var loc = IoCManager.Resolve<IPlayerLocator>();
        var player = IoCManager.Resolve<IPlayerManager>();
        var playtime = IoCManager.Resolve<PlayTimeTrackingManager>();

        var name = string.Join(' ', args).Trim();
        var data = await loc.LookupIdByNameAsync(name);

        if (data != null)
        {
            var guid = data.UserId;
            var isWhitelisted = await db.GetWhitelistStatusAsync(guid);
            if (!isWhitelisted)
            {
                shell.WriteLine(Loc.GetString("cmd-whitelistremove-existing", ("username", data.Username)));
                return;
            }

            await db.RemoveFromWhitelistAsync(guid);

            // Nyanotrasen - Update whitelist status in player data.
            if (player.TryGetPlayerDataByUsername(name, out var playerData) &&
                player.TryGetSessionByUsername(name, out var session))
            {
                playerData.ContentData()!.Whitelisted = false;
                playtime.QueueSendWhitelist(session);
            }

            shell.WriteLine(Loc.GetString("command-whitelistremove-removed", ("username", data.Username)));
            return;
        }

        shell.WriteError(Loc.GetString("cmd-whitelistremove-not-found", ("username", args[0])));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHint(Loc.GetString("cmd-whitelistremove-arg-player"));
        }

        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Ban)]
public sealed class KickNonWhitelistedCommand : LocalizedCommands
{
    public override string Command => "kicknonwhitelisted";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 0)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number-need-specific", ("properAmount", 0), ("currentAmount", args.Length)));
            shell.WriteLine(Help);
            return;
        }

        var cfg = IoCManager.Resolve<IConfigurationManager>();

        if (!cfg.GetCVar(CCVars.WhitelistEnabled))
            return;

        var player = IoCManager.Resolve<IPlayerManager>();
        var db = IoCManager.Resolve<IServerDbManager>();
        var net = IoCManager.Resolve<IServerNetManager>();

        foreach (var session in player.NetworkedSessions)
        {
            if (await db.GetAdminDataForAsync(session.UserId) is not null)
                continue;

            if (!await db.GetWhitelistStatusAsync(session.UserId))
            {
                net.DisconnectChannel(session.Channel, Loc.GetString("whitelist-not-whitelisted"));
            }
        }
    }
}
