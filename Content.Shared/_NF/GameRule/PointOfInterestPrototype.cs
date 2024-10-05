using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Content.Shared.Shuttles.Components;

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
    [DataField]
    public string Name { get; private set; } = "";

    /// <summary>
    ///     Minimum range to spawn this POI at
    /// </summary>
    [DataField]
    public int RangeMin { get; private set; } = 5000;

    /// <summary>
    ///     Maximum range to spawn this POI at
    /// </summary>
    [DataField]
    public int RangeMax { get; private set; } = 10000;

    /// <summary>
    ///     The color to display the grid and name tag as in the radar screen
    /// </summary>
    [DataField("IFFColor")]
    public Color IFFColor { get; private set; } = (100, 100, 100, 100);

    /// <summary>
    ///     Whether or not the POI is shown on IFF.
    /// </summary>
    [DataField("IFFFlags")]
    public IFFFlags Flags = IFFFlags.None;

    /// <summary>
    ///     Whether or not the POI permits IFF changes (i.e. from a console aboard it)
    /// </summary>
    [DataField]
    public bool AllowIFFChanges { get; private set; }

    /// <summary>
    ///     Whether or not the POI itself should be able to move or be moved. Should be false for immobile POIs (static stations) and true for ship-like POIs.
    /// </summary>
    [DataField]
    public bool CanMove { get; private set; }

    /// <summary>
    ///     Whether or not the POI is shown on IFF.
    /// </summary>
    [DataField]
    public GridProtectionFlags GridProtection { get; private set; } = GridProtectionFlags.None;

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

    /// <summary>
    ///     Should the public transit stop here? If true, this will be added to the list of bus stops.
    /// </summary>
    [DataField]
    public bool BusStop { get; private set; }
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
