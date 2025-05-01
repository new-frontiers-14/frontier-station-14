using Content.Shared.FixedPoint; // Frontier
using Robust.Shared.GameStates;

namespace Content.Shared.Beeper.Components;

[RegisterComponent] //component tag for events. If we add support for component pairs on events then this won't be needed anymore!
public sealed partial class ProximityBeeperComponent : Component
{
    // Frontier: imprecise search
    /// <summary>
    /// The closest that an item can be before hitting minimum interval scaling.
    /// Must be less than the detector's overall range.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 MinRange = 0;
    // End Frontier
}
