using Content.Shared.Guidebook;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;

namespace Content.Shared._NF.GameRule;

/// <summary>
///     Describes information for a single point of interest to be spawned in the world
/// </summary>
[Prototype("pointOfInterest")]
[Serializable, NetSerializable]
public sealed partial class PointOfInterestPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     The name of this point of interest
    /// </summary>
    [DataField("name")]
    public string Name { get; private set; } = "";

    /// <summary>
    ///     Minimum range to spawn this POI at
    /// </summary>
    [DataField("rangeMin")]
    public int RangeMin { get; private set; } = 5000;

    /// <summary>
    ///     Maximum range to spawn this POI at
    /// </summary>
    [DataField("rangeMax")]
    public int RangeMax { get; private set; } = 10000;

    /// <summary>
    ///     The color to display the grid and name tag as in the radar screen
    /// </summary>
    [DataField("iffColor")]
    public Color IffColor { get; private set; } = (100, 100, 100, 100);

    /// <summary>
    ///     Whether or not the POI itself should be able to move or be moved. Should be false for immobile POIs (static stations) and true for ship-like POIs.
    /// </summary>
    [DataField("canMove")]
    public bool CanMove { get; private set; }

    /// <summary>
    ///     Whether or not the POI is shown on IFF.
    /// </summary>
    [DataField("isHidden")]
    public bool IsHidden { get; private set; }

    /// <summary>
    ///     Whether or not the POI permits IFF changes (i.e. from a console aboard it)
    /// </summary>
    [DataField("allowIFFChanges")]
    public bool AllowIFFChanges { get; private set; }

    /// <summary>
    ///     Whether or not the POI is shown on IFF.
    /// </summary>
    [DataField("gridProtection")]
    public GridProtectionFlags GridProtection { get; private set; } = GridProtectionFlags.None;

    /// <summary>
    ///     Must this POI always spawn? This is independent of spawn chance. If it always spawns,
    ///     it will be excluded from any kind of random lists, for places like the sheriff's department etc.
    /// </summary>
    [DataField("alwaysSpawn")]
    public bool AlwaysSpawn { get; private set; }

    /// <summary>
    ///     If the POI does not belong to a pre-defined group, it will default to the "unique" internal category and will
    ///     use this float from 0-1 as a raw chance to spawn each round.
    /// </summary>
    [DataField("spawnChance")]
    public float SpawnChance { get; private set; } = 1;

    /// <summary>
    ///     The group that this POI belongs to. Currently, the default groups are:
    ///     "CargoDepot"
    ///     "MarketStation"
    ///     "Optional"
    ///     These three have corresponding CVARS by default, that set an optional # of this group to spawn.
    ///     Traditionally, it is 2 cargo depots, 1 trade station, and 8 optional POIs.
    ///     Dynamically added groups will default to 1 option chosen in that group, using the SpawnChance as a weighted chance
    ///     for the entire group to spawn on a per-POI basis.
    /// </summary>
    [DataField("spawnGroup")]
    public string SpawnGroup { get; private set; } = "Optional";

    /// <summary>
    ///     the path to the grid
    /// </summary>
    [DataField("gridPath", required: true)]
    public ResPath GridPath { get; private set; } = default!;
}

/// <summary>
///     A set of flags showing what events a grid should be protected form.
/// </summary>
[Flags]
public enum GridProtectionFlags : byte
{
    None = 0,
    FloorRemoval = 1,
    FloorPlacement = 2,
    RcdUse = 4, // Rapid construction device use (quickly building/deconstructing walls, windows, etc.)
    EmpEvents = 8,
    Explosions = 16,
    ArtifactTriggers = 32
}