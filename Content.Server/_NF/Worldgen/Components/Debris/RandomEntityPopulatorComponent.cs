using System.Linq;
using Content.Server.Worldgen.Systems.Debris;
using Content.Server.Worldgen.Tools;
using Content.Shared.Storage;

namespace Content.Server.Worldgen.Components.Debris;

/// <summary>
///     This is used for populating a grid with random entities automatically.
/// </summary>
[RegisterComponent, Access(typeof(RandomEntityPopulatorSystem))]
public sealed partial class RandomEntityPopulatorComponent : Component
{
    private List<(RandomEntityParameters Params, EntitySpawnCollectionCache Cache)>? _caches;

    /// <summary>
    ///     The prototype facing floor plan populator entries.
    /// </summary>
    [DataField("entries", required: true)]
    private List<RandomEntityEntrySet> _entries = default!;

    /// <summary>
    ///     The spawn collections used to place entities on different tile types.
    /// </summary>
    [ViewVariables]
    public List<(RandomEntityParameters Params, EntitySpawnCollectionCache Cache)> Caches =>
        _caches ??= _entries
            .Select(x => (x.Params, new EntitySpawnCollectionCache(x.Entries)))
            .ToList();
}

// A random set of entities to spawn
[DataDefinition]
public sealed partial class RandomEntityParameters
{
    /// <summary>
    /// The minimum number of this entity to spawn.
    /// Actual number is generated in a uniform range between min and max.
    /// Each entity is independently selected from the entity list below.
    /// </summary>
    [DataField]
    public int Min = 1;

    /// <summary>
    /// The maximum number of this entity to spawn.
    /// Actual number is generated in a uniform range.
    /// </summary>
    [DataField]
    public int Max = 1;

    /// <summary>
    /// If true, this entity set will be spawned when air sealed (e.g. under a wall).
    /// </summary>
    [DataField]
    public bool CanBeAirSealed;

    /// <summary>
    /// The probability to generate this set of entities.
    /// </summary>
    [DataField]
    public float Prob = 1.0f;
}

[DataDefinition]
public sealed partial class RandomEntityEntrySet
{
    [DataField]
    public RandomEntityParameters Params;

    [DataField]
    public List<EntitySpawnEntry> Entries;
}
