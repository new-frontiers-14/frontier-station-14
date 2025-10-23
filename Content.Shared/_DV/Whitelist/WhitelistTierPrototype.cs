using Content.Shared.Ghost.Roles;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared._DV.Whitelist;

[Prototype("whitelistTier")]
public sealed class WhitelistTierPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField]
    public string Name = string.Empty;

    [DataField]
    public Color Color = Color.White;

    [DataField]
    public List<ProtoId<JobPrototype>> Jobs = new();

    [DataField]
    public List<ProtoId<GhostRolePrototype>> GhostRoles = new();
}
