using Content.Server._NF.PublicTransit.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._NF.PublicTransit.Components;

/// <summary>
/// Added to a grid to have it act as an automated public transit bus.
/// Public Transit system will add this procedurally to any grid designated as a 'bus' through the CVAR
/// Mappers may add it to their shuttle if they wish, but this is going to force it's use and function as a public transit bus
/// </summary>
[RegisterComponent, Access(typeof(PublicTransitSystem)), AutoGenerateComponentPause]
public sealed partial class TransitShuttleComponent : Component
{
    /// <summary>
    /// The grid that the shuttle is either at or travelling to.
    /// </summary>
    [DataField]
    public EntityUid CurrentGrid;

    /// <summary>
    /// The time that the shuttle should leave for the next grid.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextTransfer;

    /// <summary>
    /// The priority tag to use for docking the shuttle.
    /// </summary>
    [DataField]
    public string? DockTag;

    /// <summary>
    /// The prototype ID for the bus route this bus covers.
    /// </summary>
    [DataField]
    public ProtoId<PublicTransitRoutePrototype> RouteId;

    /// <summary>
    /// The text to use on any screens on the bus.
    /// </summary>
    [DataField]
    public string? ScreenText;
}
