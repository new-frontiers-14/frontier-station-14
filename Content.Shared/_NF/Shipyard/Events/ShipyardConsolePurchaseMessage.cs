using Content.Shared._NF.Shipyard.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.Shipyard.Events;

/// <summary>
///     Purchase a Vessel from the console
/// </summary>
[Serializable, NetSerializable]
public sealed class ShipyardConsolePurchaseMessage : BoundUserInterfaceMessage
{
    public readonly ProtoId<VesselPrototype> Vessel;
    public readonly ProtoId<ShuttleAtmospherePrototype>? Atmosphere;

    public ShipyardConsolePurchaseMessage(ProtoId<VesselPrototype> vessel, ProtoId<ShuttleAtmospherePrototype>? atmosphere)
    {
        Vessel = vessel;
        Atmosphere = atmosphere;
    }
}
