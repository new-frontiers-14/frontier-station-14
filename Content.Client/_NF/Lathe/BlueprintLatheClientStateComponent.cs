using Content.Shared._NF.Research.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Client._NF.Lathe;

/// <summary>
/// A given client's state for a blueprint-printing lathe.
/// </summary>
[RegisterComponent]
public sealed partial class BlueprintLatheClientStateComponent : Component
{
    /// <summary>
    /// The last selected blueprint type.
    /// </summary>
    [DataField]
    public ProtoId<BlueprintPrototype>? CurrentBlueprintType;

    /// <summary>
    /// The default selected blueprint type. Can't be assigned to <c>CurrentBlueprintType</c>
    /// for dumb internal implementation reasons.
    /// </summary>
    [DataField]
    public ProtoId<BlueprintPrototype> DefaultBlueprintType = "NFAllResearchable";

    /// <summary>
    /// The last set of selected recipes.
    /// </summary>
    [DataField]
    public int[]? CurrentRecipes;
}
