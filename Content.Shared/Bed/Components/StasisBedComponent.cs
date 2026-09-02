using Content.Shared.Buckle.Components;
using Robust.Shared.GameStates;
using Content.Shared.Construction.Prototypes; // Frontier
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype; // Frontier

namespace Content.Shared.Bed.Components;

/// <summary>
/// A <see cref="StrapComponent"/> that modifies a strapped entity's metabolic rate by the given multiplier
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedBedSystem))]
public sealed partial class StasisBedComponent : Component
{
    /// <summary>
    /// What the metabolic update rate will be multiplied by (higher = slower metabolism)
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Multiplier = 10f;

    // Frontier: Upgradability fields
    [DataField("baseMultiplier", required: true), ViewVariables(VVAccess.ReadWrite)]
    public float BaseMultiplier = 10f;


    [DataField("machinePartMetabolismModifier", customTypeSerializer: typeof(PrototypeIdSerializer<MachinePartPrototype>))]
    public string MachinePartMetabolismModifier = "Capacitor";
    // End Frontier
}
