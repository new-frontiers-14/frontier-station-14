using Content.Shared.Procedural.Loot; // Frontier
using Robust.Shared.Prototypes;

namespace Content.Shared.Procedural;

[Prototype]
public sealed partial class SalvageDifficultyPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = string.Empty;

    /// <summary>
    /// Color to be used in UI.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("color")]
    public Color Color = Color.White;

    // Frontier: loot table to use
    /// <summary>
    /// The loot table prototype to use for this difficulty.
    /// If none is specified, the system's default will be used.
    /// </summary>
    [DataField]
    public ProtoId<SalvageLootPrototype>? LootTable;
    // End Frontier

    /// <summary>
    /// How much loot this difficulty is allowed to spawn.
    /// </summary>
    [DataField("lootBudget", required : true)]
    public float LootBudget;

    /// <summary>
    /// How many mobs this difficulty is allowed to spawn.
    /// </summary>
    [DataField("mobBudget", required : true)]
    public float MobBudget;

    /// <summary>
    /// Budget allowed for mission modifiers like no light, etc.
    /// </summary>
    [DataField("modifierBudget")]
    public float ModifierBudget;

    [DataField("recommendedPlayers", required: true)]
    public int RecommendedPlayers;

    // Frontier: mission types
    /// <summary>
    /// The number of structures to spawn on a destruction mission.
    /// </summary>
    [DataField]
    public int DestructionStructures = 1;
    // End Frontier: mission types
}
