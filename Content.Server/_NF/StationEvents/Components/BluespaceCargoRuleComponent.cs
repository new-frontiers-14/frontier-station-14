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
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string SpawnerPrototype = "RandomCargoSpawner";

    [DataField]
    public bool RequireSafeAtmosphere = false;

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string FlashPrototype = "EffectFlashBluespace";

    [DataField]
    public int MinimumSpawns = 1;

    [DataField]
    public int MaximumSpawns = 3;
}
