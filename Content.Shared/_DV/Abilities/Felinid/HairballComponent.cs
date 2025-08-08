using Robust.Shared.GameStates;

namespace Content.Shared._DV.Abilities.Felinid;

/// <summary>
/// Causes players to randomly vomit when trying to pick this up, or when it gets thrown at them.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedFelinidSystem))]
public sealed partial class HairballComponent : Component
{
    /// <summary>
    /// The solution to put purged chemicals into.
    /// </summary>
    [DataField]
    public string SolutionName = "hairball";

    /// <summary>
    /// Probability of someone vomiting when picking it up or getting it thrown at them.
    /// </summary>
    [DataField]
    public float VomitProb = 0.2f;
}
