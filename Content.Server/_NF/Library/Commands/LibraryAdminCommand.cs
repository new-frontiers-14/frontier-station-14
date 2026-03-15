using Content.Server.Administration;
using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._NF.Library.Commands;

/// <summary>
/// Opens the admin library panel, allowing admins to view and delete uploaded library books.
/// </summary>
[AdminCommand(AdminFlags.Admin)]
public sealed class LibraryAdminCommand : IConsoleCommand
{
    [Dependency] private readonly EuiManager _euiManager = default!;

    public string Command => "libraryadmin";
    public string Description => "Opens the admin library panel to view and delete uploaded books.";
    public string Help => "libraryadmin";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        var ui = new AdminLibraryEui();
        _euiManager.OpenEui(ui, player);
    }
}
