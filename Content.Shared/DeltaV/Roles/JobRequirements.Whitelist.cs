using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Content.Shared.Roles
{
    [UsedImplicitly]
    [Serializable, NetSerializable]
    public sealed partial class WhitelistRequirement : JobRequirement
    {
    }
}
