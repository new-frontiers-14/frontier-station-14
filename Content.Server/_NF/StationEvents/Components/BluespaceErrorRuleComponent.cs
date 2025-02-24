using Content.Server.StationEvents.Events;
using Content.Server.Shuttles.Systems;
using Content.Shared.Dataset;
using Content.Shared.Procedural;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Shared._NF.Bank.Components;
using Robust.Shared.Map;
using Content.Server._NF.StationEvents.Events;

namespace Content.Server._NF.StationEvents.Components;

[RegisterComponent, Access(typeof(BluespaceErrorRule), typeof(ShuttleSystem))]
public sealed partial class BluespaceErrorRuleComponent : Component
{
    /// <summary>
    /// Dictionary of groups where each group will have entries selected.
    /// String is just an identifier to make yaml easier.
    /// </summary>
    [DataField(required: true)] public Dictionary<string, IBluespaceSpawnGroup> Groups = new();

    /// <summary>
    /// Sector accounts and factor to be credited on event completion.
    /// Each account will be awarded with a fraction of the grid's total value at the end of the event.
    /// </summary>
    [DataField]
    public Dictionary<SectorBankAccount, float> RewardAccounts = new();

    /// <summary>
    /// The grid in question, set after starting the event
    /// </summary>
    [DataField]
    public List<EntityUid> GridsUid = new();

    /// <summary>
    /// All the added maps that should be removed on event end
    /// </summary>
    public List<MapId> MapsUid = new();

    /// <summary>
    /// If true, the grids are anchored after warping in.
    /// </summary>
    [DataField]
    public bool AnchorAfterWarp = true;

    /// <summary>
    /// If true, the grids are deleted at the end of the event.  If false, the grids are left in the map.
    /// </summary>
    [DataField]
    public bool DeleteGridsOnEnd = true;

    /// <summary>
    /// How much the grid is appraised at upon entering into existence, set after starting the event
    /// </summary>
    public double StartingValue = 0;
}

public interface IBluespaceSpawnGroup
{
    /// <summary>
    /// Minimum distance to spawn away from the station.
    /// </summary>
    public float MinimumDistance { get; }

    /// <summary>
    /// Maximum distance to spawn away from the station.
    /// </summary>
    public float MaximumDistance { get; }

    /// <summary>
    /// A localized name. Overrides other name fields.
    /// </summary>
    public List<LocId> NameLoc { get; }

    /// <summary>
    /// A dataset to pick a random name from.
    /// </summary>

    public ProtoId<LocalizedDatasetPrototype>? NameDataset { get; }

    /// <summary>
    /// The type of name the dataset holds.
    /// Determines how the name is transformed (e.g. to get "Albion-75-A" vs. "Albion NX-123")
    /// </summary>
    public BluespaceDatasetNameType NameDatasetType { get; set; }

    /// <inheritdoc />
    int MinCount { get; set; }

    /// <inheritdoc />
    int MaxCount { get; set; }

    /// <summary>
    /// Components to be added to any spawned grids.
    /// </summary>
    public ComponentRegistry AddComponents { get; set; }

    /// <summary>
    /// Should we set the metadata name of a grid. Useful for admin purposes.
    /// </summary>
    public bool NameGrid { get; set; }

    /// <summary>
    /// Should we set the warppoint name based on the grid name.
    /// </summary>
    public bool NameWarp { get; set; }

    /// <summary>
    /// Should we set the warppoint to be seen only by admins.
    /// </summary>
    public bool HideWarp { get; set; }
}

public enum BluespaceDatasetNameType
{
    FTL, // FTL names (similar to vgroids)
    Nanotrasen, // NT names (similar to shuttles)
    Verbatim, // No modification (use strings as-is)
}

[DataRecord]
public sealed class BluespaceDungeonSpawnGroup : IBluespaceSpawnGroup
{
    /// <summary>
    /// Prototypes we can choose from to spawn.
    /// </summary>
    public List<ProtoId<DungeonConfigPrototype>> Protos = new();

    /// <summary>
    /// Minimum distance from the map's origin to
    /// </summary>
    public float MinimumDistance { get; }

    public float MaximumDistance { get; }

    /// <inheritdoc />
    public List<LocId> NameLoc { get; } = new();

    /// <inheritdoc />
    public ProtoId<LocalizedDatasetPrototype>? NameDataset { get; }

    /// <inheritdoc />
    public BluespaceDatasetNameType NameDatasetType { get; set; } = BluespaceDatasetNameType.FTL;

    /// <inheritdoc />
    public int MinCount { get; set; } = 1;

    /// <inheritdoc />
    public int MaxCount { get; set; } = 1;

    /// <inheritdoc />
    public ComponentRegistry AddComponents { get; set; } = new();

    /// <inheritdoc />
    public bool NameGrid { get; set; } = false;

    /// <inheritdoc />
    public bool NameWarp { get; set; } = false; // Loads in too late, cannot name warps, use WarpPointDungeon instead.

    /// <inheritdoc />
    public bool HideWarp { get; set; } = false;
}

[DataRecord]
public sealed class BluespaceGridSpawnGroup : IBluespaceSpawnGroup
{
    public List<ResPath> Paths = new();

    /// <inheritdoc />
    public float MinimumDistance { get; }

    /// <inheritdoc />
    public float MaximumDistance { get; }
    public List<LocId> NameLoc { get; } = new();
    public ProtoId<LocalizedDatasetPrototype>? NameDataset { get; }

    /// <inheritdoc />
    public BluespaceDatasetNameType NameDatasetType { get; set; } = BluespaceDatasetNameType.FTL;
    public int MinCount { get; set; } = 1;
    public int MaxCount { get; set; } = 1;
    public ComponentRegistry AddComponents { get; set; } = new();
    public bool NameGrid { get; set; } = true;
    public bool NameWarp { get; set; } = true;
    public bool HideWarp { get; set; } = false;
}
