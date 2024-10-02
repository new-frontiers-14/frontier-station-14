using System.Diagnostics.CodeAnalysis;
using Content.Shared.CCVar;
using Content.Shared.Ghost.Roles;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Preferences;
using Robust.Shared.Utility;

namespace Content.Client.Players.PlayTimeTracking;

public sealed partial class JobRequirementsManager : ISharedPlaytimeManager
{
    public bool IsAllowed(GhostRolePrototype ghostRole, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = null;

        if (ghostRole.Whitelisted && !CheckWhitelist(ghostRole, out reason))
            return false;

        return true;
    }

    public bool CheckWhitelist(GhostRolePrototype ghostRole, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = default;
        if (!_cfg.GetCVar(CCVars.GameRoleWhitelist))
            return true;

        if (ghostRole.Whitelisted && !_jobWhitelists.Contains(ghostRole.ID) && !_whitelisted)
        {
            reason = FormattedMessage.FromUnformatted(Loc.GetString("role-not-whitelisted"));
            return false;
        }

        return true;
    }
}
