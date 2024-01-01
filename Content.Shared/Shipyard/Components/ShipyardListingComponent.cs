using Content.Shared.Shipyard.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Shipyard.Components;

/// <summary>
///   When applied to a shipyard console, adds all specified shuttles to the list of sold shuttles.
///   Can also override the name of the console.
/// </summary>
[RegisterComponent]
public sealed partial class ShipyardListingComponent : Component
{
    [ViewVariables, DataField(customTypeSerializer: typeof(PrototypeIdListSerializer<VesselPrototype>))]
    public List<string> Shuttles = new();

    [ViewVariables, DataField("name")]
    public string? NameOverride = null;
}
