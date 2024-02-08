using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._NF.PublicTransit.Components;

/// <summary>
/// Added to a grid to have it act as an automated public transit bus.
/// Public Transit system will add this procedurally to any grid designated as a 'bus' through the CVAR
/// Mappers may add it to their shuttle if they wish, but this is going to force it's use and function as a public transit bus
/// </summary>
[RegisterComponent, Access(typeof(PublicTransitSystem))]
public sealed partial class TransitShuttleComponent : Component
{
    [DataField("nextStation")]
    public EntityUid NextStation;

    [DataField("nextTransfer", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan NextTransfer;
}
