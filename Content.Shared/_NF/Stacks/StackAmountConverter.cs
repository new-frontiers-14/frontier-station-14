using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared.Stacks;
public struct StackAmount
{
    public int MaxCount;
    public int Amount;
    public bool Hidden;
}

[ImplicitDataDefinitionForInheritors]
public abstract partial class StackAmountConverter
{

    /// <summary>
    /// Converts a stack amount.
    /// </summary>
    public abstract void Convert(ref StackAmount amount);
}
