namespace Content.Server._NF.GC.Components;

/// <summary>
/// Denotes entities that have been counted as idle by the idle deletion census.
/// Once they have been counted a given number of times, they will be deleted.
/// </summary>
[RegisterComponent]
public sealed partial class DeletionCensusTallyComponent : Component
{
    /// <summary>
    /// The number of times this entity has been counted as idle (on an unloaded chunk/in FTL).
    /// </summary>
    [DataField]
    public int ConsecutivePasses;
}
