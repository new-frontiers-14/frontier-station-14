using Content.Server.Administration;
using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Shared.Administration;
using Content.Shared.Mind;
using Content.Shared.Players;
using Robust.Shared.Console;
using Robust.Shared.Map;

namespace Content.Server.Corvax.Debug;

[AdminCommand(AdminFlags.Debug)]
public sealed class SpawnThrowArtifactItemCommand : IConsoleCommand
{
    [Dependency] private readonly EntityManager _manager = default!;

    public string Command => "spawnthrowartifactitem";

    public string Description => "Spawns item that can emit ThrowArtifact.";

    public string Help => $"Usage: {Command}";

    public void Execute(IConsoleShell console, string arg, string[] args)
    {
        if (console.Player is null || !_manager.TryGetComponent<MindComponent>(console.Player.GetMind(), out var mind) || mind.CurrentEntity is null)
            return;

        var entity = _manager.SpawnEntity("PlushieVulp", new EntityCoordinates(mind.CurrentEntity.Value, new()));

        _manager.AddComponent<ThrowArtifactComponent>(entity);
    }
}
