using Content.Server._NF.PublicTransit.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.PublicTransit.Components;

/// <summary>
/// Added to a grid to have it act as an automated public transit bus.
/// Public Transit system will add this procedurally to any grid designated as a 'bus' through the CVAR
/// Mappers may add it to their shuttle if they wish, but this is going to force it's use and function as a public transit bus
/// </summary>
[RegisterComponent, Access(typeof(PublicTransitSystem)), AutoGenerateComponentPause]
public sealed partial class SectorPublicTransitComponent : Component
{
    [DataField]
    public Dictionary<ProtoId<PublicTransitRoutePrototype>, PublicTransitRoute> Routes = new();
    [DataField]
    public bool StationsGenerated = false;
    [DataField]
    public bool RoutesCreated = false;
    [DataField]
    public TimeSpan UpdatePeriod = TimeSpan.FromSeconds(2);
    [DataField, AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;
}

[Serializable]
public sealed class PublicTransitRoute(PublicTransitRoutePrototype prototype)
{
    /// <summary>
    /// The prototype this route is based off of.
    /// </summary>
    [DataField]
    public PublicTransitRoutePrototype Prototype = prototype;

    /// <summary>
    /// The list of grids this route stops at sorted by relative order.
    /// </summary>
    [DataField]
    public SortedList<int, EntityUid> GridStops = new();

    /// <summary>
    /// The relative order (key in GridStops) and index of each stop by its UID
    /// </summary>
    [DataField]
    public Dictionary<EntityUid, (int stopOrder, int stopIndex)> StopIndicesByGrid = new();
}
