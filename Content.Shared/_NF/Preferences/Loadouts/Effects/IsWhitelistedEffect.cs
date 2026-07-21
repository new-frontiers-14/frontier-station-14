using System.Diagnostics.CodeAnalysis;
using Content.Shared._NF.Whitelist;
using Content.Shared.Players;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Preferences.Loadouts.Effects;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared._NF.Preferences.Loadouts.Effects;

public sealed partial class IsWhitelistedLoadoutEffect : LoadoutEffect
{

    public override bool Validate(HumanoidCharacterProfile profile,
        RoleLoadout loadout,
        ICommonSession? session,
        IDependencyCollection collection,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        if (session == null)
        {
            reason = FormattedMessage.FromMarkupOrThrow(Loc.GetString("frontier-loadout-item-whitelisted-error"));
            return false;
        }
        var checker = collection.Resolve<IGlobalWhitelistCheck>();
        var whitelisted = checker.IsUserWhitelisted(session.UserId);
        reason = whitelisted
            ? null
            : FormattedMessage.FromMarkupOrThrow(Loc.GetString("frontier-loadout-item-not-whitelisted"));
        return whitelisted;
    }
}
