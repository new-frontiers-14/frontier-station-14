using Content.Shared.Stacks;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._NF.Contraband.Components;

/// <summary>
/// This is used for identifying contraband items and can be added to items through yml
/// </summary>
[RegisterComponent]
public sealed partial class ContrabandComponent : Component
{
    /// <summary>
    /// The value of the contraband. Defaults to 1 for easy addition to any number of items.
    /// </summary>
    [DataField("value")]
    public int Value = 1;

    /// <summary>
    /// The currency stack prototype ID to spawn as reward.
    /// </summary>
    [DataField("currency", customTypeSerializer: typeof(PrototypeIdSerializer<StackPrototype>))]
    public string Currency = "FrontierUplinkCoin";
}
