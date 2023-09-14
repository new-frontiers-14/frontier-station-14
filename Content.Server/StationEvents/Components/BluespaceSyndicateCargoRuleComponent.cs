using Content.Server.StationEvents.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.StationEvents.Components;

/// <summary>
/// This is used for an event that spawns syndicate crate
/// somewhere random on the station.
/// </summary>
[RegisterComponent, Access(typeof(BluespaceSyndicateCrateRuleComponent))]
public sealed partial class BluespaceSyndicateCrateRuleComponent : Component
{
    [DataField("syndicateCrateSpawnerPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string SyndicateCrateSpawnerPrototype = "CrateSyndicateLightSurplusBundle";

    [DataField("crateFlashPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string CrateFlashPrototype = "EffectFlashBluespace";
}
