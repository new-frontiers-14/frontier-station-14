using Content.Shared.Chat;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Players;

namespace Content.Server.Chat.Managers
{
    public interface IChatManager
    {
        /// <summary>
        ///     Keys identifying messages sent by a specific player, used when sending
        ///     <see cref="MsgChatMessage"/>
        /// </summary>
        Dictionary<IPlayerSession, int> SenderKeys { get; }

        /// <summary>
        ///     Tracks which entities a player was attached to while sending messages.
        /// </summary>
        Dictionary<IPlayerSession, HashSet<NetEntity>> SenderEntities { get; }

        void Initialize();

        /// <summary>
        ///     Dispatch a server announcement to every connected player.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="colorOverride">Override the color of the message being sent.</param>
        void DispatchServerAnnouncement(string message, Color? colorOverride = null);

        void DispatchServerMessage(ICommonSession player, string message, bool suppressLog = false);

        void TrySendOOCMessage(IPlayerSession player, string message, OOCChatType type);

        void SendHookOOC(string sender, string message);
        void SendAdminAnnouncement(string message);
        void SendAdminAlert(string message);
        void SendAdminAlert(EntityUid player, string message);

        void ChatMessageToOne(ChatChannel channel, string message, string wrappedMessage, EntityUid source, bool hideChat,
            INetChannel client, Color? colorOverride = null, bool recordReplay = false, string? audioPath = null, float audioVolume = 0, int? senderKey = null);

        void ChatMessageToMany(ChatChannel channel, string message, string wrappedMessage, EntityUid source, bool hideChat, bool recordReplay,
            IEnumerable<INetChannel> clients, Color? colorOverride = null, string? audioPath = null, float audioVolume = 0);

        void ChatMessageToManyFiltered(Filter filter, ChatChannel channel, string message, string wrappedMessage, EntityUid source, bool hideChat, bool recordReplay, Color? colorOverride, string? audioPath = null, float audioVolume = 0);

        void ChatMessageToAll(ChatChannel channel, string message, string wrappedMessage, EntityUid source, bool hideChat, bool recordReplay, Color? colorOverride = null, string? audioPath = null, float audioVolume = 0, int? senderKey = null);

        bool MessageCharacterLimit(IPlayerSession player, string message);

        void DeleteMessagesBy(IPlayerSession player);
    }
}
