using Robust.Shared.Prototypes;

namespace Content.Server._NF.Salvage;

/// <summary>
///     This component exists as a sort of stateful marker for a
///     killswitch meant to keep salvage mobs from doing stuff they
///     really shouldn't (attacking station).
///     The main thing is that adding this component ties the mob to
///     whatever it's currently parented to.
/// </summary>
[RegisterComponent]
public sealed partial class NFSalvageMobRestrictionsComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public EntityUid LinkedGridEntity = EntityUid.Invalid;

    /// <summary>
    /// If set to false, this mob will not be despawned when its linked entity is despawned.
    /// Useful for event ghost roles, for instance.
    /// </summary>
    [DataField]
    public bool DespawnIfOffLinkedGrid = false;

    // On walking off grid
    [DataField]
    public string LeaveGridPopup = "dungeon-boss-grid-warning";

    /// <summary>
    /// Components to be added when the mob leave the grid.
    /// </summary>
    [DataField]
    public ComponentRegistry AddComponentsLeaveGrid { get; set; } = new();

    /// <summary>
    /// Components to be removed when the mob leave the grid.
    /// </summary>
    [DataField]
    public ComponentRegistry RemoveComponentsLeaveGrid { get; set; } = new();

    /// <summary>
    /// Components to be added when the mob return to the grid.
    /// </summary>
    [DataField]
    public ComponentRegistry AddComponentsReturnGrid { get; set; } = new();

    /// <summary>
    /// Components to be removed when the mob return to the grid.
    /// </summary>
    [DataField]
    public ComponentRegistry RemoveComponentsReturnGrid { get; set; } = new();

    // On death
    /// <summary>
    /// Components to be added on death.
    /// </summary>
    [DataField]
    public ComponentRegistry AddComponentsOnDeath { get; set; } = new();

    /// <summary>
    /// Components to be removed on death.
    /// </summary>
    [DataField]
    public ComponentRegistry RemoveComponentsOnDeath { get; set; } = new();

    /// <summary>
    /// Components to be added on revivel.
    /// </summary>
    [DataField]
    public ComponentRegistry AddComponentsOnRevival { get; set; } = new();

    /// <summary>
    /// Components to be removed on revival.
    /// </summary>
    [DataField]
    public ComponentRegistry RemoveComponentsOnRevival { get; set; } = new();
}
