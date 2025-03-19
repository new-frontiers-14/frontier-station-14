using Content.Server._NF.PublicTransit.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.PublicTransit.Components;

/// <summary>
/// Added to a grid to have it act as an automated public transit bus.
/// Public Transit system will add this procedurally to any grid designated as a 'bus' through the CVAR
/// Mappers may add it to their shuttle if they wish, but this is going to force it's use and function as a public transit bus
/// </summary>
[RegisterComponent, Access(typeof(PublicTransitSystem))]
public sealed partial class BusScheduleComponent : Component
{
    // The route ID to use when looking up the information.
    // If left null, will be associated with the first route in the station.
    [DataField]
    public ProtoId<PublicTransitRoutePrototype>? RouteID;
}
