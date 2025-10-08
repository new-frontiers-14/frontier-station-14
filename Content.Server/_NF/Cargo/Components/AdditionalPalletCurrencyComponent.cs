using Content.Shared.Stacks;
using Content.Shared.Storage;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.Cargo.Components;

/// <summary>
/// This is used for spawning additional currency upon sale of an entity
/// </summary>
[RegisterComponent]
public sealed partial class AdditionalPalletCurrencyComponent : Component
{
    /// <summary>
    ///     The stack prototype to spawn when the item is sold.
    /// </summary>
    [DataField(required: true)] public ProtoId<StackPrototype> Currency;
    
    /// <summary>
    ///     The amount of entities to spawn.
    /// </summary>
    [DataField] public int Amount = 1;
    
    /// <summary>
    ///     The probability that the entity will spawn.
    /// </summary>
    [DataField("prob")] public float SpawnProbability = 1;
}
