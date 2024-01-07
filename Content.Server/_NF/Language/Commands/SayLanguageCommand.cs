using Content.Server.Chat.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Enums;

namespace Content.Server.Language.Commands;

[AnyCommand]
public sealed class SayLanguageCommand : IConsoleCommand
{
    public string Command => "lsay";
    public string Description => "Send chat languages to the local channel or a specific chat channel, in a specific language.";
    public string Help => "lsay <language id> <text>";

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

        if (args.Length < 2)
            return;

        var languageId = args[0];
        var message = string.Join(" ", args, startIndex: 1, count: args.Length - 1).Trim();

        if (string.IsNullOrEmpty(message))
            return;

        var languages = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<LanguageSystem>();
        var chats = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ChatSystem>();

        var language = languages.GetLanguage(languageId);
        if (language == null || !languages.CanSpeak(playerEntity, language.ID))
        {
            shell.WriteError($"Language {languageId} is invalid or you cannot speak it!");
            return;
        }

        chats.TrySendInGameICMessage(playerEntity, message, InGameICChatType.Speak, ChatTransmitRange.Normal, false, shell, player, languageOverride: language);
    }
}
