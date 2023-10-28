using Content.Server.Chat.Systems;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Enums;

namespace Content.Server._Park.Chat.Commands
{
    [AnyCommand]
    internal sealed class ESayCommand : IConsoleCommand
    {
        public string Command => "esay";
        public string Description => "Send chat messages to Shadowkin.";
        public string Help => $"{Command} <text>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (shell.Player is not IPlayerSession player)
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

            if (args.Length < 1)
                return;

            var message = string.Join(" ", args).Trim();
            if (string.IsNullOrEmpty(message))
                return;

            EntitySystem.Get<ChatSystem>().TrySendInGameICMessage(playerEntity, message, InGameICChatType.Empathy, false, false, shell, player, checkRadioPrefix: false);
        }
    }
}
