using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.Atmos.BUIStates;

[Serializable, NetSerializable]
public sealed class GaslockConsoleBoundUserInterfaceState(NetCoordinates coords, GaslockState state)
    : BoundUserInterfaceState
{
    public NetCoordinates Coords = coords;
    public GaslockState State = state;
}

[Serializable, NetSerializable]
public sealed class GaslockState(Dictionary<NetEntity, List<GaslockPortState>> docks)
{
    public Dictionary<NetEntity, List<GaslockPortState>> Docks = docks;
}
