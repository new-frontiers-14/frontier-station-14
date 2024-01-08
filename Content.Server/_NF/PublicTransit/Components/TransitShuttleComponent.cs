using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._NF.PublicTransit.Components;

[RegisterComponent, Access(typeof(PublicTransitSystem))]
public sealed partial class TransitShuttleComponent : Component
{
    [DataField("nextStation")]
    public EntityUid NextStation;

    [DataField("nextTransfer", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan NextTransfer;
}
