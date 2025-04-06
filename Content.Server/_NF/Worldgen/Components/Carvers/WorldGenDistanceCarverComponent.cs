namespace Content.Server._NF.Worldgen.Components.Carvers;

/// <summary>
/// This denotes an entity that spawns fewer asteroids around it.
/// </summary>
[RegisterComponent]
public sealed partial class WorldGenDistanceCarverComponent : Component
{
    /// <summary>
    /// The probability that something within a given distance is generated.
    /// No need to be ordered.
    /// </summary>
    [DataField]
    public List<WorldGenDistanceThreshold> DistanceThresholds = new();

    /// <summary>
    /// The probability that something within a given squared distance is generated.
    /// For internal use, _must_ be ordered in descending order of distance.
    /// </summary>
    [ViewVariables]
    public List<WorldGenDistanceThreshold> SquaredDistanceThresholds = new();
}

[DataDefinition]
public sealed partial class WorldGenDistanceThreshold
{
    /// <summary>
    /// The maximum distance within the threshold.
    /// </summary>
    [DataField]
    public float MaxDistance;

    /// <summary>
    /// The probability that something within this distance will spawn.
    /// </summary>
    [DataField]
    public float Prob = 1.0f;
}
