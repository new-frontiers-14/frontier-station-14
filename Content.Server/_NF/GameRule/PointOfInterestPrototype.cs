using Content.Server.GameTicking.Presets;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

namespace Content.Server._NF.GameRule;

/// <summary>
///     Describes information for a single point of interest to be spawned in the world
/// </summary>
[Prototype]
[Serializable]
public sealed partial class PointOfInterestPrototype : IPrototype, IInheritingPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<PointOfInterestPrototype>))]
    public string[]? Parents { get; private set; }

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }

    /// <summary>
    ///     The name of this point of interest
    /// </summary>
    [DataField(required: true)]
    public string Name { get; private set; } = "";

    /// <summary>
    ///     Should we set the warppoint name based on the grid name.
    /// </summary>
    [DataField]
    public bool NameWarp { get; set; } = true;

    /// <summary>
    ///     If true, makes the warp point admin-only (hiding it for players).
    /// </summary>
    [DataField]
    public bool HideWarp { get; set; } = false;

    /// <summary>
    ///     Minimum range to spawn this POI at
    /// </summary>
    [DataField]
    public int MinimumDistance { get; private set; } = 5000;

    /// <summary>
    ///     Maximum range to spawn this POI at
    /// </summary>
    [DataField]
    public int MaximumDistance { get; private set; } = 10000;

    /// <summary>
    ///     Components to be added to any spawned grids.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public ComponentRegistry AddComponents { get; set; } = new();

    /// <summary>
    ///     What gamepresets ID this POI is allowed to spawn on.
    ///     If left empty, all presets are allowed.
    /// </summary>
    [DataField]
    public ProtoId<GamePresetPrototype>[] SpawnGamePreset { get; private set; } = [];

    /// <summary>
    ///     If the POI does not belong to a pre-defined group, it will default to the "unique" internal category and will
    ///     use this float from 0-1 as a raw chance to spawn each round.
    /// </summary>
    [DataField]
    public float SpawnChance { get; private set; } = 1;

    /// <summary>
    ///     The group that this POI belongs to. Currently, the default groups are:
    ///     "CargoDepot"
    ///     "MarketStation"
    ///     "Required"
    ///     "Optional"
    ///     Each POI labeled in the Required group will be spawned in every round.
    ///     Apart from that, each of thesehave corresponding CVARS by default, that set an optional # of this group to spawn.
    ///     Traditionally, it is 2 cargo depots, 1 trade station, and 8 optional POIs.
    ///     Dynamically added groups will default to 1 option chosen in that group, using the SpawnChance as a weighted chance
    ///     for the entire group to spawn on a per-POI basis.
    /// </summary>
    [DataField]
    public string SpawnGroup { get; private set; } = "Optional";

    /// <summary>
    ///     the path to the grid
    /// </summary>
    [DataField(required: true)]
    public ResPath GridPath { get; private set; } = default!;
}
