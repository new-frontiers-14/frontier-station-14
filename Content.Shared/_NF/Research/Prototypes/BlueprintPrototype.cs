using Robust.Shared.Prototypes;

namespace Content.Shared._NF.Research.Prototypes;

/// <summary>
/// This is a prototype for a type of blueprint.
/// </summary>
[Prototype("blueprint")]
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
    /// The name of a given blueprint of this type, should be a locale string accepting the {$name} tag.
    /// </summary>
    [DataField]
    public LocId EntityName = string.Empty;

    /// <summary>
    /// The description of the blueprint, should be a locale string accepting the {$name} tag.
    /// </summary>
    [DataField]
    public LocId Description = string.Empty;
}