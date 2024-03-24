using Content.Shared.Shipyard.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Shipyard.Components;

/// <summary>
///   When applied to a shipyard console, adds all specified shuttles to the list of sold shuttles.
/// </summary>
[RegisterComponent]
public sealed partial class ShipyardListingComponent : Component
{
    /// <summary>
    ///   All VesselPrototype IDs that should be listed in this shipyard console.
    /// </summary>
    [ViewVariables, DataField(customTypeSerializer: typeof(PrototypeIdListSerializer<VesselPrototype>))]
    public List<string> Shuttles = new();
}
