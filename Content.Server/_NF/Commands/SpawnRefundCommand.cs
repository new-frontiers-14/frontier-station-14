using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Server.Stack;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Content.Shared.Popups;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._NF.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class SpawnRefundCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IEntitySystemManager _entity = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;

    private static readonly EntProtoId CashPrototypeId = "SpaceCash";

    public string Command => "spawnrefund";

    public string Description => "Spawns an exact number of spesos to be given as a refund. You must be a ghost with a free hand.";

    public string Help => $"${Command} <amount> [reason]";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is not (1 or 2))
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (shell.Player is not { } player)
        {
            shell.WriteError("Could not find the player executing the command");
            return;
        }

        if (player.AttachedEntity is not { } uid)
        {
            shell.WriteError("Could not find your attached entity");
            return;
        }

        // By allowing only ghosts to spawn refunds, we reduce the risk of badmins
        // spawning themselves random money whenever they need it.
        if (!_entityManager.HasComponent<GhostComponent>(uid))
        {
            shell.WriteError("You must be an aghost to spawn a refund");
            return;
        }

        if (!int.TryParse(args[0], out var amount))
        {
            shell.WriteError($"Could not parse the amount '{args[0]}' as an integer");
            return;
        }
        if (amount <= 0)
        {
            shell.WriteError($"Refund amount must be greater than zero; attempted to spawn {amount} spesos");
            return;
        }
        args.TryGetValue(1, out var reason);

        var refund = _entityManager.Spawn(CashPrototypeId);
        _entity.GetEntitySystem<StackSystem>().SetCount(refund, amount);

        if (!_entity.GetEntitySystem<HandsSystem>().TryPickupAnyHand(uid, refund))
        {
            shell.WriteError("You must have an empty hand");
            _entity.GetEntitySystem<PopupSystem>().PopupEntity("You must have an empty hand", uid, player, PopupType.MediumCaution);
            _entityManager.DeleteEntity(refund);
            return;
        }

        _adminLog.Add(LogType.AdminRefund, LogImpact.Medium,
            $"{_entityManager.ToPrettyString(uid)} spawned a refund of {amount} spesos, {_entityManager.ToPrettyString(refund)}. Reason: {reason}");
        shell.WriteLine($"Spawned a refund of {amount} spesos");
    }
}
