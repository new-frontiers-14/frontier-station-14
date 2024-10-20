using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeltaV.Whitelist;

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
}
