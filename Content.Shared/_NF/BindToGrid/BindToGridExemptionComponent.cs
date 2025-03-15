using Robust.Shared.GameStates;

namespace Content.Shared._NF.BindToGrid;

/// <summary>
/// Exempts this entity from any variation system that would bind its functionality to a single grid
/// </summary>
[RegisterComponent]
public sealed partial class BindToGridExemptionComponent : Component
{
}
