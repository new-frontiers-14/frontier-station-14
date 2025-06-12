using Content.Shared._NF.Shipyard.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.PublicTransit.Prototypes;

[Prototype]
public sealed partial class PublicTransitRoutePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Bus route number.  Buses will receive this name.
    /// </summary>
    [DataField(required: true)]
    public int RouteNumber { get; private set; } = default!;

    /// <summary>
    /// The number of stations to spawn an additional bus on this route.  Non-positive numbers will imply there is only one bus on the route.
    /// </summary>
    [DataField]
    public int StationsPerBus { get; private set; } = 0;

    /// <summary>
    /// The amount of time to spend in FTL between stations.
    /// </summary>
    [DataField]
    public TimeSpan TravelTime { get; private set; } = TimeSpan.FromSeconds(80);

    /// <summary>
    /// The amount of time to spend in FTL between stations.
    /// </summary>
    [DataField]
    public TimeSpan WaitTime { get; private set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// The string to use as a dock tag.
    /// </summary>
    [DataField]
    public string? DockTag { get; private set; } = null;

    /// <summary>
    /// The 
    /// </summary>
    [DataField]
    public EntProtoId? SignEntity { get; private set; } = null;

    /// <summary>
    /// The possible bus types to spawn on this route.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<VesselPrototype>> BusVessels { get; private set; } = default!;

    /// <summary>
    /// The color of related bus livery.
    /// </summary>
    [DataField]
    public Color LiveryColor { get; private set; } = default!;
}
