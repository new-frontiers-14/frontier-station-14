namespace Content.Server._NF.GC.Components;

/// <summary>
/// Denotes entities that are exempt from the deletion census.
/// Optionally propagates to child grids when split.
/// </summary>
[RegisterComponent]
public sealed partial class DeletionCensusExemptComponent : Component
{
    /// <summary>
    /// If true, on map grids, this will propagate the census to other grids.
    /// Prevents shuttle or station deletion on mishaps.
    /// </summary>
    [DataField]
    public bool PassOnGridSplit;
}
