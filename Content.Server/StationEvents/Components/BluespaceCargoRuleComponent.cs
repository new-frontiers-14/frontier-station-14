using Content.Server.StationEvents.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.StationEvents.Components;

/// <summary>
/// This is used for an event that spawns an cargo
/// somewhere random on the station.
/// </summary>
[RegisterComponent, Access(typeof(BluespaceCargoRule))]
public sealed class BluespaceCargoRuleComponent : Component
{
    [DataField("cargoSpawnerPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string CargoSpawnerPrototype = "RandomArtifactSpawner";

    [DataField("cargoFlashPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string CargoFlashPrototype = "EffectFlashBluespace";

    [DataField("possibleSightings")]
    public List<string> PossibleSighting = new()
    {
        "bluespace-cargo-sighting-1",
        "bluespace-cargo-sighting-2",
        "bluespace-cargo-sighting-3",
        "bluespace-cargo-sighting-4",
        "bluespace-cargo-sighting-5",
        "bluespace-cargo-sighting-6",
        "bluespace-cargo-sighting-7"
    };
}
