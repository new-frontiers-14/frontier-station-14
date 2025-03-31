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
    /// The name of the blueprint, should be a locale string accepting the {$name} tag.
    /// </summary>
    [DataField]
    public LocId Name = string.Empty;

    /// <summary>
    /// The description of the blueprint, should be a locale string accepting the {$name} tag.
    /// </summary>
    [DataField]
    public LocId Description = string.Empty;

    /// <summary>
    /// The type of machines that can accept this blueprint.  Used to categorize blueprints in the lathe.
    /// </summary>
    [DataField]
    public BlueprintType Type = BlueprintType.Autolathe;
}

[Flags]
public enum BlueprintType : byte
{
    Autolathe = 1 << 0,
    Engineering = 1 << 1,
    Medical = 1 << 2,
    Mercenary = 1 << 3,
    Nfsd = 1 << 4,
    Salvage = 1 << 5,
    Service = 1 << 6,
}
