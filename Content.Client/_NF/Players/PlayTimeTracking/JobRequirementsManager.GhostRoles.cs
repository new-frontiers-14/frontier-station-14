using System.Diagnostics.CodeAnalysis;
using Content.Shared.CCVar;
using Content.Shared.Ghost.Roles;
using Content.Shared.Players.PlayTimeTracking;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Players.PlayTimeTracking;

public sealed partial class JobRequirementsManager : ISharedPlaytimeManager
{
    public bool IsAllowed(GhostRolePrototype ghostRole, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = null;

        // Frontier: if/when ghost role bans are added (per-role/blanket ban?)
        if (_roleBans.Contains("GhostRoles"))
        {
            reason = FormattedMessage.FromUnformatted(Loc.GetString("role-ban"));
            return false;
        }

        if (!CheckWhitelist(ghostRole, out reason))
            return false;

        return true;
    }

    public bool CheckWhitelist(ProtoId<GhostRolePrototype> ghostRoleId, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = default;
        if (!_cfg.GetCVar(CCVars.GameRoleWhitelist))
            return true;

        // No ghost role found.
        if (!_prototypes.TryIndex(ghostRoleId, out var ghostRole))
        {
            reason = FormattedMessage.FromUnformatted(Loc.GetString("role-not-found"));
            return false;
        }

        if (ghostRole.Whitelisted && !_jobWhitelists.Contains(ghostRole.ID))
        {
            reason = FormattedMessage.FromUnformatted(Loc.GetString("role-not-whitelisted"));
            return false;
        }

        return true;
    }

    public bool CheckWhitelist(GhostRolePrototype ghostRole, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = default;
        if (!_cfg.GetCVar(CCVars.GameRoleWhitelist))
            return true;

        if (ghostRole.Whitelisted && !_jobWhitelists.Contains(ghostRole.ID))
        {
            reason = FormattedMessage.FromUnformatted(Loc.GetString("role-not-whitelisted"));
            return false;
        }

        return true;
    }
}
