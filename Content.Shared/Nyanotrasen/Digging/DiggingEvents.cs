using Content.Shared.DoAfter;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Nyanotrasen.Digging;


[Serializable, NetSerializable]
public sealed partial class EarthDiggingDoAfterEvent : DoAfterEvent
{
    public NetCoordinates Coordinates { get; set; }

    private EarthDiggingDoAfterEvent(){}

    public EarthDiggingDoAfterEvent(NetCoordinates coordinates)
    {
        Coordinates = coordinates;
    }
    public override DoAfterEvent Clone()
    {
        return this;
    }
}

[Serializable, NetSerializable]
public sealed class EarthDiggingCancelledEvent : EntityEventArgs
{
    public NetEntity Shovel;
}
