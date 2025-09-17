using Content.Server.Administration;
using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._NF.Administration.Commands;

[AdminCommand(AdminFlags.Moderator)]
public sealed class PlayTimeEditorPanelCommand : LocalizedCommands
{
    [Dependency] private readonly EuiManager _euis = default!;

    public override string Command => "playtimeeditorpanel";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        _euis.OpenEui(new PlayTimeEditorPanelEui(), player);
    }
}
