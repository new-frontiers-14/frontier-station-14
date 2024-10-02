using Content.Server.Corvax.Respawn;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared._NF.CCVar;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.NF14.Commands;

[AnyCommand()]
public sealed class GhostRespawnCommand : IConsoleCommand
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IEntitySystemManager _entity = default!;

    public string Command => "ghostrespawn";
    public string Description => "Allows the player to return to the lobby if they've been dead long enough, allowing re-entering the round AS ANOTHER CHARACTER.";
    public string Help => $"{Command}";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (!_configurationManager.GetCVar(NFCCVars.RespawnEnabled))
        {
            shell.WriteLine("Respawning is disabled, ask an admin to respawn you.");
            return;
        }

        if (shell.Player is null)
        {
            shell.WriteLine("You cannot run this from the console!");
            return;
        }

        if (shell.Player.AttachedEntity is null)
        {
            shell.WriteLine("You cannot run this in the lobby, or without an entity.");
            return;
        }

        if (!_entityManager.TryGetComponent<GhostComponent>(shell.Player.AttachedEntity, out var ghost))
        {
            shell.WriteLine("You are not a ghost.");
            return;
        }

        var respawnResetTime = _entity.GetEntitySystem<RespawnSystem>().GetRespawnTime(shell.Player.UserId);

        if (respawnResetTime is not null)
        {
            if (_gameTiming.CurTime < respawnResetTime.Value)
            {
                var timeLeft = (respawnResetTime.Value - _gameTiming.CurTime).TotalSeconds;
                shell.WriteLine($"You haven't been dead long enough. You can respawn in {timeLeft} seconds.");
                return;
            }
        }

        var gameTicker = _entityManager.EntitySysManager.GetEntitySystem<GameTicker>();
        gameTicker.Respawn(shell.Player);
    }
}
