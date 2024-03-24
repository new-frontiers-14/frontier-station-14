namespace Content.Server._NF.PublicTransit.Components;

/// <summary>
/// Added to a station that is available for public transit.
/// </summary>
[RegisterComponent, Access(typeof(PublicTransitSystem))]
public sealed partial class StationTransitComponent : Component
{
}
