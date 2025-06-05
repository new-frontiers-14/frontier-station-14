using System.Net;
using System.Threading.Tasks;
using Content.Server.Administration.Systems;
using Content.Shared.Administration;
using Robust.Server.ServerStatus;
using Robust.Shared.Network;
using Robust.Shared.Utility;
using Content.Server.Chat.Managers;
using Content.Shared.Chat;

namespace Content.Server.Administration;


public sealed partial class ServerApi : IPostInjectInit
{

    [Dependency] private readonly IChatManager _chatManager = default!;


    private async Task ActionSendBwoink(IStatusHandlerContext context)
    {
        var body = await ReadJson<BwoinkActionBody>(context);
        if (body == null)
            return;

        await RunOnMainThread(async () =>
    {
        // Player not online or wrong Guid
        if (!_playerManager.TryGetSessionById(new NetUserId(body.Guid), out var player))
        {
            await RespondError(
                context,
                ErrorCode.PlayerNotFound,
                HttpStatusCode.UnprocessableContent,
                "Player not found");
            return;
        }

        var serverBwoinkSystem = _entitySystemManager.GetEntitySystem<BwoinkSystem>();
        var message = new SharedBwoinkSystem.BwoinkTextMessage(player.UserId, SharedBwoinkSystem.SystemUserId, body.Text, adminOnly: body.AdminOnly);
        serverBwoinkSystem.OnWebhookBwoinkTextMessage(message, body);

        // Respond with OK
        await RespondOk(context);
    });


    }

    private async Task ActionSendMessage(IStatusHandlerContext context)
    {

        // Body: Name, Message, Guid, ShowTag, ChatType

        var body = await ReadJson<AdminChatBody>(context);
        if (body == null)
            return;

        await RunOnMainThread(async () =>
        {
            switch (body.ChatType)
            {
                case 0: // Admin Chat
                    // This currently does not have any Logging, Once Upstream PR#33840 is merged, it will use that Chat Function instead which has build in logging.
                    if (body.Username == null || body.Username == "")
                    {
                        await RespondBadRequest(context, "Username must be supplied for admin chat");
                        return;
                    }
                    var adminMessage = Loc.GetString("chat-manager-send-admin-chat-wrap-message", ("adminChannelName", Loc.GetString("chat-manager-admin-channel-name")), ("playerName", body.ShowTag ? "(DC) " + body.Username : body.Username), ("message", FormattedMessage.EscapeText(body.Message)));
                    _chatManager.ChatMessageToAll(ChatChannel.AdminChat, body.Message, adminMessage, source: EntityUid.Invalid, hideChat: false, recordReplay: false);
                    break;
                case 1: // Admin Alert
                    _chatManager.SendAdminAlert(body.ShowTag ? "(DC) " + body.Message : body.Message);
                    break;
                case 2: // OOC Chat
                    if (body.Username == null || body.Username == "")
                    {
                        await RespondBadRequest(context, "Username must be supplied for OOC chat");
                        return;
                    }
                    var oocMessage = Loc.GetString("chat-manager-send-ooc-wrap-message", ("playerName", body.ShowTag ? "(DC) " + body.Username : body.Username), ("message", FormattedMessage.EscapeText(body.Message)));
                    _chatManager.ChatMessageToAll(ChatChannel.OOC, body.Message, oocMessage, source: EntityUid.Invalid, hideChat: false, recordReplay: false);
                    break;
                case 3: // Server "Chat"
                    var serverMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", FormattedMessage.EscapeText(body.Message)));
                    _chatManager.ChatMessageToAll(ChatChannel.Server, body.Message, serverMessage, source: EntityUid.Invalid, hideChat: false, recordReplay: false);
                    break;
                default:
                    await RespondBadRequest(context, "Invalid chat type");
                    return;
            }
            await RespondOk(context);
        });
    }



    #region  Action Bodies

    public sealed class BwoinkActionBody
    {
        public required string Text { get; init; }
        public required string Username { get; init; }
        public required Guid Guid { get; init; }
        public bool UserOnly { get; init; }
        public required bool WebhookUpdate { get; init; }
        public bool AdminOnly { get; init; }
    }

    public sealed class AdminChatBody
    {
        public required string Message { get; init; }
        public required int ChatType { get; init; }
        public string? Username { get; init; }
        public bool ShowTag { get; init; } = true;
    }

    #endregion

}
