using Content.Shared.Random;
using Robust.Shared.Prototypes;
using Content.Shared.Whitelist; // Frontier

namespace Content.Shared.Mining.Components;

/// <summary>
/// Defines an entity that will drop a random ore after being destroyed.
/// </summary>
[RegisterComponent]
public sealed partial class OreVeinComponent : Component
{
    /// <summary>
    /// How often an entity will be seeded with ore. Note: the amount of ore
    /// that is dropped is dependent on the ore prototype. <see crefalso="OrePrototype"/>
    /// </summary>
    [DataField]
    public float OreChance = 0.1f;

    /// <summary>
    /// The weighted random prototype used for determining what ore will be dropped.
    /// </summary>
    [DataField]
    public ProtoId<WeightedRandomOrePrototype>? OreRarityPrototypeId;

    /// <summary>
    /// The ore that this entity holds.
    /// If set in the prototype, it will not be overriden.
    /// </summary>
    [DataField]
    public ProtoId<OrePrototype>? CurrentOre;

    /// <summary>
    /// Frontier: if this ore is somehow "ruined", set this to true before destroying the entity.
    /// </summary>
    [DataField]
    public bool PreventSpawning;

    /// <summary>
    /// Frontier: whitelist to check when gathering materials - these entities are too strong and ruin the ore.
    /// </summary>
    [DataField]
    public EntityWhitelist? GatherDestructionWhitelist;
}
