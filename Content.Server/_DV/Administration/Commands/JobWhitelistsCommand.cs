using Content.Server.Administration;
using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._DV.Administration.Commands;

/// <summary>
/// Opens the job whitelists panel for editing player whitelists.
/// To use this ingame it's easiest to first open the player panel, then hit Job Whitelists.
/// </summary>
[AdminCommand(AdminFlags.Whitelist)]
public sealed class JobWhitelistsCommand : LocalizedCommands
{
    [Dependency] private readonly EuiManager _eui = default!;
    [Dependency] private readonly IPlayerLocator _locator = default!;

    public override string Command => "jobwhitelists";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not {} player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        if (args.Length != 1)
        {
            shell.WriteLine(Loc.GetString("cmd-ban-invalid-arguments"));
            shell.WriteLine(Help);
        }

        var located = await _locator.LookupIdByNameOrIdAsync(args[0]);
        if (located is null)
        {
            shell.WriteError(Loc.GetString("cmd-jobwhitelists-player-err"));
            return;
        }

        var ui = new JobWhitelistsEui(located.UserId, located.Username);
        ui.LoadWhitelists();
        _eui.OpenEui(ui, player);
    }
}
