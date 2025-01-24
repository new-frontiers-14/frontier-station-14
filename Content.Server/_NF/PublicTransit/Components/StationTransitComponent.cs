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
    /// The list of routes that will service this station.
    /// </summary>
    public List<ProtoId<PublicTransitRoutePrototype>> Routes = new();
}
