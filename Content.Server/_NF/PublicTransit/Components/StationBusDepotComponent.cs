namespace Content.Server._NF.PublicTransit.Components;

/// <summary>
/// A component that adds all existing bus routes to this station.
/// </summary>
[RegisterComponent, Access(typeof(PublicTransitSystem))]
public sealed partial class StationBusDepotComponent : Component;
