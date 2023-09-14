using Content.Server.StationEvents.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.StationEvents.Components;

/// <summary>
/// This is used for an event that spawns cargo
/// somewhere random on the station.
/// </summary>
[RegisterComponent, Access(typeof(BluespaceCargoRule))]
public sealed partial class BluespaceCargoRuleComponent : Component
{
    [DataField("cargoSpawnerPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string CargoSpawnerPrototype = "RandomCargoSpawner";

    [DataField("cargoGenericSpawnerPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string CargoGenericSpawnerPrototype = "RandomCargoGenericSpawner";

    [DataField("cargoFlashPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string CargoFlashPrototype = "EffectFlashBluespace";
}
