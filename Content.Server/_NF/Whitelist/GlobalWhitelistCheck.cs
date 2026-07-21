using Content.Server.Players.JobWhitelist;
using Content.Shared._NF.Whitelist;
using Robust.Shared.Network;

namespace Content.Server._NF.Whitelist;

public sealed class GlobalWhitelistCheck : IGlobalWhitelistCheck
{

    [Dependency] private JobWhitelistManager _jobWhitelistManager = default!;

    /// <inheritdoc/>
    public bool IsUserWhitelisted(NetUserId netUser)
    {
        return _jobWhitelistManager.IsGloballyWhitelisted(netUser);
    }
}
