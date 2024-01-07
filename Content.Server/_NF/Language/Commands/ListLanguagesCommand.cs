using System.Linq;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Enums;

namespace Content.Server.Language.Commands;

[AnyCommand]
public sealed class ListLanguagesCommand : IConsoleCommand
{
    public string Command => "lslangs";
    public string Description => "List languages your current entity can speak at the current moment.";
    public string Help => "lslangs";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError("This command cannot be run from the server.");
            return;
        }

        if (player.Status != SessionStatus.InGame)
            return;

        if (player.AttachedEntity is not {} playerEntity)
        {
            shell.WriteError("You don't have an entity!");
            return;
        }

        var languages = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<LanguageSystem>();

        var (spokenLangs, knownLangs) = languages.GetAllLanguages(playerEntity);

        shell.WriteLine("Spoken: " + string.Join(", ", spokenLangs));
        shell.WriteLine("Understood: " + string.Join(", ", knownLangs));
    }
}
