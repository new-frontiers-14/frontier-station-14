namespace Content.Server._NF.Stacks.Components;

/// <summary>
/// Denotes an item that starts with a random amount of material in its stack.
/// The material is uniformly picked from an inclusive minimum and maximum.
/// </summary>
[RegisterComponent]
public sealed partial class RandomStackCountComponent : Component
{
    [DataField]
    public int Min;

    [DataField]
    public int Max;
}
