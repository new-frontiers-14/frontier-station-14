using Robust.Shared.Serialization;

namespace Content.Shared._Starlight.Atmos.Piping.Unary.Visuals;

[Serializable, NetSerializable]
public enum GasInletSiphonVisuals : byte
{
    State,
}

[Serializable, NetSerializable]
public enum GasInletSiphonState : byte
{
    UnpoweredOff,
    Off,
    UnpoweredOn,
    On,
}

