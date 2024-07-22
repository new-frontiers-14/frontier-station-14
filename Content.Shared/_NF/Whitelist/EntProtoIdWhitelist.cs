using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Whitelist;

/// <summary>
///     Used to determine whether an entity fits a certain whitelist by ID set.
///     Does not whitelist by prototypes, since that is undesirable; you're better off just adding a tag to all
///     entity prototypes that need to be whitelisted, and checking for that.
/// </summary>
/// <code>
/// whitelist:
///   ids:
///   - Cigarette
///   - FirelockElectronics
///   matchParents: false
/// </code>
[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class EntProtoIdWhitelist
{
    /// <summary>
    ///     Entity IDs that are allowed in the whitelist.
    /// </summary>
    [DataField("id")] public List<string>? Ids;

    /// <summary>
    ///     If false, an entity must be a direct match.  If true, check against the entity's parents.
    /// </summary>
    [DataField]
    public bool MatchParents = false;
}
