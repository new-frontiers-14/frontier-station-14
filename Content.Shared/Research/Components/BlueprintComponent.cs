using Content.Shared._NF.Lathe;
using Content.Shared.Research.Prototypes;
using Content.Shared.Research.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Research.Components;

/// <summary>
/// This is used for an item that is inserted directly into a given lathe to provide it with a recipe.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(BlueprintSystem), typeof(SharedBlueprintLatheSystem))] // Frontier: add SharedBlueprintLatheSystem access
[AutoGenerateComponentState] // Frontier: dynamically set blueprints
public sealed partial class BlueprintComponent : Component
{
    /// <summary>
    /// The recipes that this blueprint provides.
    /// </summary>
    [DataField(required: true)]
    [AutoNetworkedField] // Frontier: dynamically set blueprints
    public HashSet<ProtoId<LatheRecipePrototype>> ProvidedRecipes = new();
}
