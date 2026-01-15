using Content.Shared.Chat;

namespace Content.Client.Chat.Managers
{
    public interface IChatManager : ISharedChatManager
    {
        public event Action? PermissionsUpdated;
        public void SendMessage(string text, ChatSelectChannel channel);
        public void UpdatePermissions();
    }
}
