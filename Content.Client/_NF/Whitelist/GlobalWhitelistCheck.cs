using Content.Client.Players.PlayTimeTracking;
using Content.Shared._NF.Whitelist;
using Robust.Shared.Network;

namespace Content.Client._NF.Whitelist;

public sealed class GlobalWhitelistCheck : IGlobalWhitelistCheck
{

    [Dependency] private JobRequirementsManager _requirementsManager = default!;

    /// <inheritdoc/>>
    public bool IsUserWhitelisted(NetUserId netUser)
    {
        //No arguments needed because the client only cares about one whitelist: its own.
        return _requirementsManager.IsWhitelisted();
    }
}
