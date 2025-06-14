namespace Content.Server._NF.GC.Components;

// Denotes items that are important elements of moth cuisine, and should be edible by moths.
[RegisterComponent]
public sealed partial class DeletionPassTallyComponent : Component
{
    /// <summary>
    /// The number of times this entity has been seen on an unloaded chunk.
    /// </summary>
    [DataField]
    public int ConsecutivePasses;
}
