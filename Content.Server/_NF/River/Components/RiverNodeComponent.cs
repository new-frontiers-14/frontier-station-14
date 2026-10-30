using Content.Shared._NF.River.Components;

namespace Content.Server._NF.River.Components;

[RegisterComponent]
public sealed partial class RiverNodeComponent : SharedRiverNodeComponent
{
    /// <summary>
    /// The amount of extra units of velocity perfect utilisation of the river provides.
    /// </summary>
    [DataField]
    public float Boost = 0.5f; //Temporary, was 0.3f

    /// <summary>
    /// The smallest the Slowdown Multiplier can be when flying perfectly against the flow.
    /// </summary>
    [DataField]
    public float SlowdownMultiplier = 0.5f;

    /// <summary>
    /// Whether this node is a river source or not.
    /// </summary>
    [DataField]
    public bool IsSource = false;

    [DataField]
    public Direction CardinalFlow = Direction.East;
}


