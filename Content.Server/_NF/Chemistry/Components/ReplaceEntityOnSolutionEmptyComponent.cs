using Robust.Shared.Prototypes;

namespace Content.Server.Nutrition.EntitySystems;

[RegisterComponent]
public sealed partial class ReplaceEntityOnSolutionEmptyComponent : Component
{
    /// <summary>
    /// The solution to check for entity replacement.
    /// </summary>
    [DataField]
    public string Solution = "solution";

    /// <summary>
    /// The entity to replace the existing one.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId ReplacementEntity;
}
