using System.Diagnostics.CodeAnalysis;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Roles;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Preferences.Loadouts.Effects;

/// <summary>
/// Checks for a job requirement to be met such as playtime.
/// </summary>
public sealed partial class RoleBlacklistLoadoutEffect : LoadoutEffect
{
    [DataField(required: true)]
    public List<ProtoId<RoleLoadoutPrototype>> Blacklist = default!;

    public override bool Validate(HumanoidCharacterProfile profile, RoleLoadout loadout, ICommonSession? session, IDependencyCollection collection, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        if (Blacklist.Contains(loadout.Role))
        {
            reason = new FormattedMessage();
            reason.TryAddMarkup(Loc.GetString("role-blacklist-loadout-invalid"), out var _);
            return false;
        }
        reason = null;
        return true;
    }
}
