using Content.Shared._NF.Salvage.Expeditions.Modifiers;
using Content.Shared.CCVar;
using Content.Shared.Dataset;
using Content.Shared.Procedural;
using Content.Shared.Procedural.Loot;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Salvage.Expeditions;
using Content.Shared.Salvage.Expeditions.Modifiers;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Shared.Salvage;

public abstract partial class SharedSalvageSystem : EntitySystem
{
    [Dependency] protected readonly IConfigurationManager CfgManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    /// <summary>
    /// Main loot table for salvage expeditions.
    /// </summary>
    public static readonly ProtoId<SalvageLootPrototype> ExpeditionsLootProto = "NFSalvageLootModerate"; // Frontier: SalvageLoot<NFSalvageLootModerate

    public abstract bool ResolveExpedition(EntityUid? uid, ref SharedSalvageExpeditionComponent? component); // Frontier
    public string GetFTLName(LocalizedDatasetPrototype dataset, int seed)
    {
        var random = new System.Random(seed);
        return $"{Loc.GetString(dataset.Values[random.Next(dataset.Values.Count)])}-{random.Next(10, 100)}-{(char) (65 + random.Next(26))}";
    }

    public SalvageMission GetMission(SalvageMissionType config, SalvageDifficultyPrototype difficulty, int seed) // Frontier: add config
    {
        // This is on shared to ensure the client display for missions and what the server generates are consistent
        var modifierBudget = difficulty.ModifierBudget;
        var rand = new System.Random(seed);

        // Run budget in order of priority
        // - Biome
        // - Lighting
        // - Atmos
        var biome = GetMod<SalvageBiomeModPrototype>(rand, ref modifierBudget);
        var light = GetBiomeMod<SalvageLightMod>(biome.ID, rand, ref modifierBudget);
        var temp = GetBiomeMod<SalvageTemperatureMod>(biome.ID, rand, ref modifierBudget);
        var air = GetBiomeMod<SalvageAirMod>(biome.ID, rand, ref modifierBudget);
        // Frontier - Moved so faction gets priority over dungeon
        // var dungeon = GetBiomeMod<SalvageDungeonModPrototype>(biome.ID, rand, ref modifierBudget);

        // Frontier: restrict factions per difficulty
        // var factionProtos = _proto.EnumeratePrototypes<SalvageFactionPrototype>().ToList();
        var factionProtos = _proto.EnumeratePrototypes<SalvageFactionPrototype>()
            .Where(x =>
                {
                    return !x.Configs.TryGetValue("Difficulties", out var difficulties)
                        || string.IsNullOrWhiteSpace(difficulties)
                        || difficulties.Split(",").Contains(difficulty.ID.ToString());
                }
            ).ToList();
        // End Frontier: difficulties per faction
        factionProtos.Sort((x, y) => string.Compare(x.ID, y.ID, StringComparison.Ordinal));
        var faction = factionProtos[rand.Next(factionProtos.Count)];
        // Frontier - Moved so faction gets priority over dungeon. GetBiomeMod -> GetFactionMod
        var dungeon = GetDungeonMod<SalvageDungeonModPrototype>(biome.ID, faction.ID, rand, ref modifierBudget);

        var mods = new List<string>();

        if (air.Description != string.Empty)
        {
            mods.Add(Loc.GetString(air.Description));
        }

        // only show the description if there is an atmosphere since wont matter otherwise
        if (temp.Description != string.Empty && !air.Space)
        {
            mods.Add(Loc.GetString(temp.Description));
        }

        if (light.Description != string.Empty)
        {
            mods.Add(Loc.GetString(light.Description));
        }

        var duration = TimeSpan.FromSeconds(CfgManager.GetCVar(CCVars.SalvageExpeditionDuration));

        return new SalvageMission(seed, dungeon.ID, faction.ID, biome.ID, air.ID, temp.Temperature, light.Color, duration, mods, difficulty.ID, config); // Frontier: add difficulty.ID, config
    }

    public T GetBiomeMod<T>(string biome, System.Random rand, ref float rating) where T : class, IPrototype, IBiomeSpecificMod
    {
        var mods = _proto.EnumeratePrototypes<T>().ToList();
        mods.Sort((x, y) => string.Compare(x.ID, y.ID, StringComparison.Ordinal));
        rand.Shuffle(mods);

        foreach (var mod in mods)
        {
            if (mod.Cost > rating || (mod.Biomes != null && !mod.Biomes.Contains(biome)))
                continue;

            rating -= mod.Cost;

            return mod;
        }

        throw new InvalidOperationException();
    }

    // Frontier - Faction specific dungeons
    public T GetDungeonMod<T>(string biome, string faction, System.Random rand, ref float rating) where T : class, IPrototype, IFactionSpecificMod
    {
        var mods = _proto.EnumeratePrototypes<T>().ToList();
        mods.Sort((x, y) => string.Compare(x.ID, y.ID, StringComparison.Ordinal));
        rand.Shuffle(mods);

        // Separate dungeon mods which include the current faction into a primary list.
        var matchingMods = mods.Where(x => x.Factions != null && x.Factions.Contains(faction));

        // Separate dungeon mods with no specified factions into a secondary list.
        // Anything else (i.e. dungeons with a faction list that did not include this faction) is discarded.
        var otherMods = mods.Where(x => x.Factions == null);

        // Pick from the list of matching factions first
        foreach (var mod in matchingMods)
        {
            // Still have to check for cost and biome. If a dungeon has the correct faction but is not allowed to generate in this biome,
            // it will fall back onto faction-unspecified selection to find a dungeon that is.
            if (mod.Cost > rating || mod.Biomes != null && !mod.Biomes.Contains(biome))
                continue;

            rating -= mod.Cost;

            return mod;
        }

        foreach (var mod in otherMods)
        {
            if (mod.Cost > rating ||
                mod.Biomes != null && !mod.Biomes.Contains(biome))
                continue;

            rating -= mod.Cost;

            return mod;
        }

        throw new InvalidOperationException();
    }
    // Frontier end

    public T GetMod<T>(System.Random rand, ref float rating) where T : class, IPrototype, ISalvageMod
    {
        var mods = _proto.EnumeratePrototypes<T>().ToList();
        mods.Sort((x, y) => string.Compare(x.ID, y.ID, StringComparison.Ordinal));
        rand.Shuffle(mods);

        foreach (var mod in mods)
        {
            if (mod.Cost > rating)
                continue;

            rating -= mod.Cost;

            return mod;
        }

        throw new InvalidOperationException();
    }
}

// Frontier: salvage mission type
[Serializable, NetSerializable]
public enum SalvageMissionType : byte
{
    /// <summary>
    /// Destroy the specified structures in a dungeon.
    /// </summary>
    Destruction = 0,

    /// <summary>
    /// Kill a large creature in a dungeon.
    /// </summary>
    Elimination = 1,

    /// <summary>
    /// Maximum value for random generation, should not be used directly.
    /// </summary>
    Max = Elimination,
}
// End Frontier
