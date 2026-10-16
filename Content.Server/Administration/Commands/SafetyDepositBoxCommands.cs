using Content.Server.Database;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class CleanStaleSafetyDepositBoxesCommand : IConsoleCommand
{
    [Dependency] private readonly IServerDbManager _db = default!;

    public string Command => "cleanstalesafetyboxes";
    public string Description => "Deletes safety deposit boxes that have been withdrawn and have no items for more than the specified number of days.";
    public string Help => "cleanstalesafetyboxes <days>\nExample: cleanstalesafetyboxes 7\nDeletes boxes that have been withdrawn for more than 7 days with no items.";

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError("Usage: cleanstalesafetyboxes <days>");
            return;
        }

        if (!int.TryParse(args[0], out var days) || days <= 0)
        {
            shell.WriteError("Days must be a positive integer.");
            return;
        }

        shell.WriteLine($"Searching for safety deposit boxes that have been withdrawn for more than {days} days with no items...");

        try
        {
            var count = await _db.DeleteStaleSafetyDepositBoxes(days);
            shell.WriteLine($"Successfully deleted {count} stale safety deposit box(es).");
        }
        catch (Exception ex)
        {
            shell.WriteError($"Error cleaning stale boxes: {ex.Message}");
        }
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHint("days (e.g., 7)");
        }

        return CompletionResult.Empty;
    }
}
