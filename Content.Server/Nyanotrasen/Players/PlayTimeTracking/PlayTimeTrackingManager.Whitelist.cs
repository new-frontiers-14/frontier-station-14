using Content.Shared.Players;
using Content.Shared.Players.PlayTimeTracking;
using Robust.Shared.Player;

namespace Content.Server.Players.PlayTimeTracking;

public sealed partial class PlayTimeTrackingManager
{
    private void SendWhitelistCached(ICommonSession playerSession)
    {
        var whitelist = playerSession.ContentData()?.Whitelisted ?? false;

        var msg = new MsgWhitelist
        {
            Whitelisted = whitelist
        };

        _net.ServerSendMessage(msg, playerSession.ConnectedClient);
    }

    /// <summary>
    /// Queue sending whitelist status to the client.
    /// </summary>
    public void QueueSendWhitelist(ICommonSession player)
    {
        if (DirtyPlayer(player) is { } data)
            data.NeedRefreshWhitelist = true;
    }
}
