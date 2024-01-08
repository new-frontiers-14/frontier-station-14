using System.Linq;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Enums;

namespace Content.Server.Language.Commands;

[AnyCommand]
public sealed class SelectLanguageCommand : IConsoleCommand
{
    public string Command => "lsselectlang";
    public string Description => "Open a menu to select a langauge to speak.";
    public string Help => "lsselectlang";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError("This command cannot be run from the server.");
            return;
        }

        if (player.Status != SessionStatus.InGame)
            return;

        if (player.AttachedEntity is not { } playerEntity)
        {
            shell.WriteError("You don't have an entity!");
            return;
        }

        if (args.Length < 1)
            return;

        var languageId = args[0];

        var languages = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<LanguageSystem>();

        var language = languages.GetLanguage(languageId);
        if (language == null || !languages.CanSpeak(playerEntity, language.ID))
        {
            shell.WriteError($"Language {languageId} is invalid or you cannot speak it!");
            return;
        }

        languages.SetLanguage(playerEntity, language.ID);
    }
}
