using System.Diagnostics.CodeAnalysis;
using Content.Shared.Humanoid;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared.Preferences.Loadouts.Effects;

/// <summary>
/// Checks for a profile to be within a particular set of sexes.
/// </summary>
public sealed partial class SexLoadoutEffect : LoadoutEffect
{
    [DataField("sex", required: true)]
    public List<Sex> Sexes = default!;

    public override bool Validate(HumanoidCharacterProfile profile, RoleLoadout loadout, ICommonSession? session, IDependencyCollection collection, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        if (Sexes.Contains(profile.Sex))
        {
            reason = null;
            return true;
        }
        reason = new FormattedMessage();
        reason.TryAddMarkup(Loc.GetString("sex-loadout-invalid"), out var _);
        return false;
    }
}
