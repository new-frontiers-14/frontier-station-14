using Robust.Shared.GameStates;
using System.Numerics;

namespace Content.Shared._NF.River.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RiverNodeComponent : Component
{
    /// <summary>
    /// The location of the Node for display purposes.
    /// </summary>
    [AutoNetworkedField]
    public Vector2 Location;

    /// <summary>
    /// A list of nodes which this node flows into.
    /// </summary>
    [AutoNetworkedField]
    public List<NetEntity> NextNodes = new();

    /// <summary>
    /// A list of nodes which flow into this node.
    /// </summary>
    [AutoNetworkedField]
    public List<NetEntity> PriorNodes = new();

    /// <summary>
    /// The mesh of the river segments that spring from this node. For Mass Scanner rendering.
    /// </summary>
    [AutoNetworkedField]
    public Vector2[] SegmentMesh;

    /// <summary>
    /// The direction in which the river flows.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Angle FlowDirection = 0d;

    /// <summary>
    /// The distance from the Node that Shuttles are influenced by its effects.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float NodeRange = 50.0f; //TODO: Figure out a reasonable value

    /// <summary>
    /// The amount of extra units of velocity perfect utilisation of the river provides.
    /// </summary>
    [DataField]
    public float Boost = 1.3f; //Temporary, was 0.3f

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

    /// <summary>
    /// Influences whether rivers are keen to merge or split. Higher numbers wish to split rather than merge.
    /// Lower numbers wish to merge rather than split.
    /// </summary>
    public float Saturation = 1f;

    [AutoNetworkedField]
    public Vector2 ForwardControlPoint;

}
