using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.Atmos.BUIStates;

[Serializable, NetSerializable]
public sealed class GaslockConsoleBoundUserInterfaceState : BoundUserInterfaceState
{
    public NetCoordinates Coords;
    public GaslockState State;

    public GaslockConsoleBoundUserInterfaceState(NetCoordinates coords, GaslockState state)
    {
        Coords = coords;
        State = state;
    }
}

[Serializable, NetSerializable]
public sealed class GaslockState
{
    public Dictionary<NetEntity, List<GaslockPortState>> Docks;

    public GaslockState(Dictionary<NetEntity, List<GaslockPortState>> docks)
    {
        Docks = docks;
    }
}
