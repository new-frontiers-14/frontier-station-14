using System.Linq;
using Content.Server.Administration;
using Content.Server._NF.Shipyard.Systems;
using Content.Server.Station.Systems;
using Content.Shared._NF.CCVar;
using Content.Shared._NF.Shipyard.Prototypes;
using Content.Shared.Administration;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._NF.Shipyard.Commands;

/// <summary>
/// Purchases a shuttle and docks it to a station.
/// </summary>
[AdminCommand(AdminFlags.Fun)]
public sealed class PurchaseShuttleCommand : IConsoleCommand
{
    [Dependency] private readonly IEntitySystemManager _entityManager = default!;
    [Dependency] private readonly IResourceManager _resourceManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;

    public string Command => "purchaseshuttle";
    public string Description => Loc.GetString("shipyard-commands-purchase-desc");
    public string Help => IsCustomAtmosEnabled ? $"{Command} <station ID> <gridfile path> [atmosphere ID]" : $"{Command} <station ID> <gridfile path>";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (!int.TryParse(args[0], out var stationId))
        {
            shell.WriteError($"{args[0]} is not a valid integer.");
            return;
        }

        var shuttlePath = args[1];
        ShuttleAtmospherePrototype? atmosphere = null;
        if (IsCustomAtmosEnabled && args.Length > 2)
        {
            if (!_prototypeManager.TryIndex(args[2], out atmosphere))
            {
                shell.WriteError($"{args[2]} is not a valid shuttle atmosphere prototype.");
                return;
            }
        }

        var system = _entityManager.GetEntitySystem<ShipyardSystem>();
        var station = new EntityUid(stationId);
        system.TryPurchaseShuttle(station, new ResPath(shuttlePath), out _, atmosphere);
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        switch (args.Length)
        {
            case 1:
            {
                var stationSystem = _entityManager.GetEntitySystem<StationSystem>();
                var opts = stationSystem.GetStationNames()
                    .Select(station => new CompletionOption(station.Entity.ToString(), station.Name));
                return CompletionResult.FromHintOptions(opts, Loc.GetString("station-id"));
            }
            case 2:
            {
                var prefix = args[1];
                var opts = CompletionHelper.UserFilePath(prefix, _resourceManager.UserData)
                    .Concat(CompletionHelper.ContentFilePath(prefix, _resourceManager));
                return CompletionResult.FromHintOptions(opts, Loc.GetString("cmd-hint-savemap-path"));
            }
            case 3:
            {
                if (!IsCustomAtmosEnabled)
                {
                    return CompletionResult.Empty;
                }
                var opts = _prototypeManager.GetInstances<ShuttleAtmospherePrototype>()
                    .Select(proto => new CompletionOption(proto.Value.ID, proto.Value.Name));
                return CompletionResult.FromHintOptions(opts, Loc.GetString("shipyard-commands-purchase-atmos-hint"));
            }
        }

        return CompletionResult.Empty;
    }

    private bool IsCustomAtmosEnabled => _configurationManager.GetCVar(NFCCVars.ShipyardCustomAtmos);
}
