using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.GameStates;
using Content.Shared.Chemistry.Reagent; // Frontier
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Chemistry.Components;

/// <summary>
/// Passively increases a solution's quantity of a reagent.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause, AutoGenerateComponentState, NetworkedComponent]
[Access(typeof(SolutionRegenerationSystem))]
public sealed partial class SolutionRegenerationComponent : Component
{
    /// <summary>
    /// The name of the solution to add to.
    /// </summary>
    [DataField("solution", required: true)]
    public string SolutionName = string.Empty;

    /// <summary>
    /// The solution to add reagents to.
    /// </summary>
    [ViewVariables]
    public Entity<SolutionComponent>? SolutionRef = null;

    /// <summary>
    /// The reagent(s) to be regenerated in the solution.
    /// </summary>
    [DataField(required: true)]
    [Access(typeof(SolutionRegenerationSystem), Other = AccessPermissions.ReadExecute)]
    public Solution Generated = default!;

    /// <summary>
    /// Frontier: Levels of reagents to stop creating more of each at (optional).
    /// Use if you want to ensure an even mix of reagents
    /// </summary>
    [DataField]
    public List<ReagentQuantity>? UpperLimits = default!;

    /// <summary>
    /// How long it takes to regenerate once.
    /// </summary>
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The time when the next regeneration will occur.
    /// </summary>
    [DataField("nextChargeTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField, AutoNetworkedField]
    [Access(typeof(SolutionRegenerationSystem), Other = AccessPermissions.ReadWrite)]

    public TimeSpan NextRegenTime;
}
