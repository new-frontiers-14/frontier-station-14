using Content.Server._NF.PublicTransit.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.PublicTransit.Components;

/// <summary>
/// Added to a grid that is stopped at by public transit.
/// </summary>
[RegisterComponent, Access(typeof(PublicTransitSystem))]
public sealed partial class StationTransitComponent : Component
{
    /// <summary>
    /// The list of routes that will service this station and the relative stop order along that route.
    /// Stops are ordered in increasing value (lower numbers, earlier on the list)
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<PublicTransitRoutePrototype>, int> Routes = new();
}
