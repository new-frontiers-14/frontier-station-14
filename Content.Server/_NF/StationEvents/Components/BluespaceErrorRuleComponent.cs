using Content.Server.StationEvents.Events;
using Content.Shared.Storage;
using Content.Server.Shuttles.Systems;
using Content.Shared.Dataset;
using Content.Shared.Procedural;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(BluespaceErrorRule), (typeof(ShuttleSystem)))]
public sealed partial class BluespaceErrorRuleComponent : Component
{
    /// <summary>
    /// Dictionary of groups where each group will have entries selected.
    /// String is just an identifier to make yaml easier.
    /// </summary>
    [DataField(required: true)] public Dictionary<string, IBluespaceSpawnGroup> Groups = new();

    /// <summary>
    /// The color of your thing. The name should be set by the mapper when mapping.
    /// </summary>
    [DataField]
    public Color Color = new Color(225, 15, 155);

    /// <summary>
    /// Multiplier to apply to the remaining value of a grid, to be deposited in the station account for defending
    /// </summary>
    [DataField]
    public float RewardFactor = 0f;

    /// <summary>
    /// The grid in question, set after starting the event
    /// </summary>
    [DataField]
    public EntityUid? GridUid = null;

    /// <summary>
    /// How much the grid is appraised at upon entering into existence, set after starting the event
    /// </summary>
    [DataField]
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

    /// <inheritdoc />
    public ProtoId<DatasetPrototype>? NameDataset { get; }

    /// <inheritdoc />
    int MinCount { get; set; }

    /// <inheritdoc />
    int MaxCount { get; set; }

    /// <summary>
    /// Components to be added to any spawned grids.
    /// </summary>
    public ComponentRegistry AddComponents { get; set; }

    /// <summary>
    /// Hide the IFF label of the grid.
    /// </summary>
    public bool Hide { get; set; }

    /// <summary>
    /// Should we set the metadata name of a grid. Useful for admin purposes.
    /// </summary>
    public bool NameGrid { get; set; }
}

[DataRecord]
public sealed class BluespaceDungeonSpawnGroup : IBluespaceSpawnGroup
{
    /// <summary>
    /// Prototypes we can choose from to spawn.
    /// </summary>
    public List<ProtoId<DungeonConfigPrototype>> Protos = new();

    /// <inheritdoc />
    public float MinimumDistance { get; }

    public float MaximumDistance { get; }

    /// <inheritdoc />
    public ProtoId<DatasetPrototype>? NameDataset { get; }

    /// <inheritdoc />
    public int MinCount { get; set; } = 1;

    /// <inheritdoc />
    public int MaxCount { get; set; } = 1;

    /// <inheritdoc />
    public ComponentRegistry AddComponents { get; set; } = new();

    /// <inheritdoc />
    public bool Hide { get; set; } = false;

    /// <inheritdoc />
    public bool NameGrid { get; set; } = false;
}

[DataRecord]
public sealed class BluespaceGridSpawnGroup : IBluespaceSpawnGroup
{
    public List<ResPath> Paths = new();

    /// <inheritdoc />
    public float MinimumDistance { get; }

    /// <inheritdoc />
    public float MaximumDistance { get; }
    public ProtoId<DatasetPrototype>? NameDataset { get; }
    public int MinCount { get; set; } = 1;
    public int MaxCount { get; set; } = 1;
    public ComponentRegistry AddComponents { get; set; } = new();
    public bool Hide { get; set; } = false;
    public bool NameGrid { get; set; } = true;
}
