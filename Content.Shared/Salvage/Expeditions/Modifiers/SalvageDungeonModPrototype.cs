using Content.Shared._NF.Salvage.Expeditions.Modifiers; // Frontier - Faction specific dungeons
using Content.Shared.Procedural;
using Robust.Shared.Prototypes;

namespace Content.Shared.Salvage.Expeditions.Modifiers;

[Prototype]
public sealed partial class SalvageDungeonModPrototype : IPrototype, IFactionSpecificMod
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField("desc")] public LocId Description { get; private set; } = string.Empty;

    /// <inheridoc/>
    [DataField("cost")]
    public float Cost { get; private set; } = 0f;

    /// <inheridoc/>
    [DataField]
    public List<ProtoId<SalvageBiomeModPrototype>>? Biomes { get; private set; } = null;

    // Frontier - Faction specific dungeons
    /// <inheridoc/>
    [DataField]
    public List<ProtoId<SalvageFactionPrototype>>? Factions { get; private set; } = null;
    // Frontier end

    /// <summary>
    /// The config to use for spawning the dungeon.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<DungeonConfigPrototype> Proto = string.Empty;
}
