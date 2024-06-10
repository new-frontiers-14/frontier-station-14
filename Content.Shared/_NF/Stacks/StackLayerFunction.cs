namespace Content.Shared.Stacks;


/// <summary>
/// Data used to determine which layers of a stack's sprite are visible.
/// </summary>
public struct StackLayerData
{
    public int Actual;
    public int MaxCount;
    public bool Hidden;
}

[ImplicitDataDefinitionForInheritors]
public abstract partial class StackLayerFunction
{
    /// <summary>
    /// Runs an arbitrary function on StackLayerData to adjust how it appears.
    /// </summary>
    public abstract void Apply(ref StackLayerData amount);
}
