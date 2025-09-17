using Content.Server.Administration;
using Content.Server._NF.Shipyard.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Utility;
using Content.Server._NF.GC.Components;

namespace Content.Server._NF.Shipyard.Commands;

/// <summary>
/// Purchases a shuttle and docks it to a station.
/// </summary>
[AdminCommand(AdminFlags.Fun)]
public sealed class PurchaseShuttleCommand : IConsoleCommand
{
    [Dependency] private readonly IEntitySystemManager _sysManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    public string Command => "purchaseshuttle";
    public string Description => Loc.GetString("shipyard-commands-purchase-desc");
    public string Help => $"{Command} <station ID> <gridfile path>";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (!int.TryParse(args[0], out var stationId))
        {
            shell.WriteError($"{args[0]} is not a valid integer.");
            return;
        }

        var shuttlePath = args[1];
        var system = _sysManager.GetEntitySystem<ShipyardSystem>();
        var station = new EntityUid(stationId);
        if (system.TryPurchaseShuttle(station, new ResPath(shuttlePath), out var shuttleUid))
        {
            _entityManager.EnsureComponent<DeletionCensusExemptComponent>(shuttleUid.Value); // Ensure ship doesn't get deleted, though chunks should be.
        }
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        switch (args.Length)
        {
            case 1:
                return CompletionResult.FromHint(Loc.GetString("station-id"));
            case 2:
                return CompletionResult.FromHint(Loc.GetString("cmd-hint-savemap-path"));
        }

        return CompletionResult.Empty;
    }
}
