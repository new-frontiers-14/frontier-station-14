using System.Linq;
using Content.Shared.Access;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.Access;

/// <summary>
/// Utilities to check the access of a particular entity against an arbitrary set of AccessLevels and AccessTags
/// Useful to avoid using the AccessReader component (e.g. for supporting multiple access sets)
/// </summary>
/// <remarks>
/// Does this have to be an EntitySystem?
/// </remarks>
public sealed partial class NFAccessSystemUtilities : EntitySystem
{
    [Dependency] IPrototypeManager _proto = default!;

    public bool IsAllowed(ICollection<ProtoId<AccessLevelPrototype>>? targetTags, ICollection<ProtoId<AccessLevelPrototype>>? accessTags, ICollection<ProtoId<AccessGroupPrototype>>? accessGroups)
    {
        // Empty/null sets: no access requested.
        if ((accessTags == null || accessTags.Count <= 0)
            && (accessGroups == null || accessGroups.Count <= 0))
            return true;

        // Non-empty access set, empty target set, can't fulfill membership
        if (targetTags == null || targetTags.Count <= 0)
            return false;

        // Check lists
        if (accessTags != null)
        {
            foreach (var accessTag in accessTags)
            {
                if (targetTags.Contains(accessTag))
                    return true;
            }
        }

        if (accessGroups != null)
        {
            foreach (var groupId in accessGroups)
            {
                if (!_proto.TryIndex(groupId, out var groupProto))
                    continue;

                if (groupProto.Tags.All(targetTags.Contains))
                    return true;
            }
        }
        return false;
    }
}
