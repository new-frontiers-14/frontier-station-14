using Content.Server._NF.PublicTransit.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.PublicTransit.Components;

/// <summary>
/// Represents a bus schedule for a particular route.
/// Used to inform the player about when the next bus will be at a given grid,
/// and/or when a bus will arrive at each grid on its route.
/// </summary>
[RegisterComponent, Access(typeof(PublicTransitSystem))]
public sealed partial class BusScheduleComponent : Component
{
    // The route ID to use when looking up the information.
    // If left null, will be associated with the first route in the station.
    [DataField]
    public ProtoId<PublicTransitRoutePrototype>? RouteId;
}
