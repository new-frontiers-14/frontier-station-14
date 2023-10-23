using System.Diagnostics.CodeAnalysis;
using Content.Shared.CCVar;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Roles;
using Robust.Shared.Utility;

namespace Content.Client.Players.PlayTimeTracking;

public sealed partial class JobRequirementsManager
{
    private bool _whitelisted = false;

    private void RxWhitelist(MsgWhitelist message)
    {
        _whitelisted = message.Whitelisted;
    }

    public bool IsWhitelisted()
    {
        return _whitelisted;
    }
}
