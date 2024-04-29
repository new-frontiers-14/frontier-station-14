using System.Linq;
using Content.Shared.Dataset;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Salvage.Expeditions;
using Content.Shared.Salvage.Expeditions.Modifiers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Salvage;

public abstract partial class SharedSalvageSystem : EntitySystem
{
    [Dependency] private readonly ILocalizationManager _loc = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    #region Descriptions

    public string GetMissionDescription(SalvageMission mission)
    {
        // Hardcoded in coooooz it's dynamic based on difficulty and I'm lazy.
        switch (mission.Mission)
        {
            case SalvageMissionType.Mining:
                // Taxation: , ("tax", $"{GetMiningTax(mission.Difficulty) * 100f:0}")
                return Loc.GetString("salvage-expedition-desc-mining");
            case SalvageMissionType.Destruction:
                var proto = _proto.Index<SalvageFactionPrototype>(mission.Faction).Configs["DefenseStructure"];

                return Loc.GetString("salvage-expedition-desc-structure",
                    ("count", GetStructureCount(mission.Difficulty)),
                    ("structure", _loc.GetEntityData(proto).Name));
            case SalvageMissionType.Elimination:
                return Loc.GetString("salvage-expedition-desc-elimination");
            default:
                throw new NotImplementedException();
        }
    }

    public float GetMiningTax(DifficultyRating baseRating)
    {
        return 0.6f + (int) baseRating * 0.05f;
    }

    /// <summary>
    /// Gets the amount of structures to destroy.
    /// </summary>
    public int GetStructureCount(DifficultyRating baseRating)
    {
        return 1 + (int) baseRating * 2;
    }

    #endregion

    public int GetDifficulty(DifficultyRating rating)
    {
        switch (rating)
        {
            case DifficultyRating.Minimal:
                return 4;
            case DifficultyRating.Minor:
                return 6;
            case DifficultyRating.Moderate:
                return 8;
            case DifficultyRating.Hazardous:
                return 10;
            case DifficultyRating.Extreme:
                return 12;
            default:
                throw new ArgumentOutOfRangeException(nameof(rating), rating, null);
        }
    }

    /// <summary>
    /// How many groups of mobs to spawn for a mission.
    /// </summary>
    public float GetSpawnCount(DifficultyRating difficulty)
    {
        return (int) difficulty * 2;
    }

    public static string GetFTLName(DatasetPrototype dataset, int seed)
    {
        var random = new System.Random(seed);
        return $"{dataset.Values[random.Next(dataset.Values.Count)]}-{random.Next(10, 100)}-{(char) (65 + random.Next(26))}";
    }

    public SalvageMission GetMission(SalvageMissionType config, DifficultyRating difficulty, int seed)
    {
        // This is on shared to ensure the client display for missions and what the server generates are consistent
        var rating = (float) GetDifficulty(difficulty);
        // Don't want easy missions to have any negative modifiers but also want
        // easy to be a 1 for difficulty.
        rating -= 1f;
        var rand = new System.Random(seed);

        // Run budget in order of priority
        // - Biome
        // - Lighting
        // - Atmos
        var faction = GetMod<SalvageFactionPrototype>(rand, ref rating);
        var biome = GetMod<SalvageBiomeMod>(rand, ref rating);
        var air = GetBiomeMod<SalvageAirMod>(biome.ID, rand, ref rating);
        var dungeon = GetBiomeMod<SalvageDungeonModPrototype>(biome.ID, rand, ref rating);

        var mods = new List<string>();

        if (air.Description != string.Empty)
        {
            mods.Add(air.Description);
        }

        // only show the description if there is an atmosphere since wont matter otherwise
        var temp = GetBiomeMod<SalvageTemperatureMod>(biome.ID, rand, ref rating);
        if (temp.Description != string.Empty && !air.Space)
        {
            mods.Add(temp.Description);
        }

        var light = GetBiomeMod<SalvageLightMod>(biome.ID, rand, ref rating);
        if (light.Description != string.Empty)
        {
            mods.Add(light.Description);
        }

        var time = GetMod<SalvageTimeMod>(rand, ref rating);
        // Round the duration to nearest 15 seconds.
        var exactDuration = MathHelper.Lerp(time.MinDuration, time.MaxDuration, rand.NextFloat());
        exactDuration = MathF.Round(exactDuration / 15f) * 15f;
        var duration = TimeSpan.FromSeconds(exactDuration);

        if (time.Description != string.Empty)
        {
            mods.Add(time.Description);
        }

        var rewards = GetRewards(difficulty, rand);
        return new SalvageMission(seed, difficulty, dungeon.ID, faction.ID, config, biome.ID, air.ID, temp.Temperature, light.Color, duration, rewards, mods);
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

    private List<string> GetRewards(DifficultyRating difficulty, System.Random rand)
    {
        var rewards = new List<string>(3);
        var ids = RewardsForDifficulty(difficulty);
        foreach (var id in ids)
        {
            // pick a random reward to give
            var weights = _proto.Index<WeightedRandomEntityPrototype>(id);
            rewards.Add(weights.Pick(rand));
        }

        return rewards;
    }

    /// <summary>
    /// Get a list of WeightedRandomEntityPrototype IDs with the rewards for a certain difficulty.
    /// Frontier: added uncommon and legendary reward tiers, limited amount of rewards to 1 per difficulty rating
    /// </summary>
    private string[] RewardsForDifficulty(DifficultyRating rating)
    {
        var common = "SalvageRewardCommon";
        var uncommon = "SalvageRewardUncommon";
        var rare = "SalvageRewardRare";
        var epic = "SalvageRewardEpic";
        var legendary = "SalvageRewardLegendary";
        switch (rating)
        {
            case DifficultyRating.Minimal:
                return new string[] { common };
            case DifficultyRating.Minor:
                return new string[] { uncommon };
            case DifficultyRating.Moderate:
                return new string[] { rare };
            case DifficultyRating.Hazardous:
                return new string[] { epic };
            case DifficultyRating.Extreme:
                return new string[] { legendary };
            default:
                throw new NotImplementedException();
        }
    }
}

[Serializable, NetSerializable]
public enum SalvageMissionType : byte
{
    /// <summary>
    /// No dungeon, just ore loot and random mob spawns.
    /// </summary>
    Mining,

    /// <summary>
    /// Destroy the specified structures in a dungeon.
    /// </summary>
    Destruction,

    /// <summary>
    /// Kill a large creature in a dungeon.
    /// </summary>
    Elimination,
}

[Serializable, NetSerializable]
public enum DifficultyRating : byte
{
    Minimal,
    Minor,
    Moderate,
    Hazardous,
    Extreme,
}
