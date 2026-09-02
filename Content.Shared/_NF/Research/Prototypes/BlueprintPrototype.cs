using Content.Shared.Lathe.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._NF.Research.Prototypes;

/// <summary>
/// This is a prototype for a type of blueprint.
/// </summary>
[Prototype]
public sealed partial class BlueprintPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The entity prototype ID of the blueprint to spawn.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Blueprint;

    /// <summary>
    /// The name of the blueprint type itself.
    /// </summary>
    [DataField]
    public LocId Name = string.Empty;

    /// <summary>
    /// List of packs associated with this blueprint.
    /// </summary>
    [DataField]
    public List<ProtoId<LatheRecipePackPrototype>> Packs = new();
}
