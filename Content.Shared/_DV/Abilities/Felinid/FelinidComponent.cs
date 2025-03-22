using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._DV.Abilities.Felinid;

/// <summary>
/// Felenid god component controls 3 things:
/// 1. When you use <see cref="ItemCougherComponent"/> to cough up a hairball, it purges chemicals from your bloodstream.
/// 2. Enables the cough hairball action after eating a mouse with <c>FelinidFoodComponent</c>.
/// 3. Full immunity to hairball vomiting chance.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedFelinidSystem))]
public sealed partial class FelinidComponent : Component
{
    /// <summary>
    /// Quantity of reagents to purge from the bloodstream.
    /// </summary>
    [DataField]
    public FixedPoint2 PurgedQuantity = 20;
}
