using Content.Shared.Access;
using Content.Shared.Access.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System.Numerics;

namespace Content.Shared._NF.River.Components;

[/*RegisterComponent,*/ NetworkedComponent, /*AutoGenerateComponentState*/]
public abstract partial class SharedRiverNodeComponent : Component
{
    /// <summary>
    /// The location of the Node for display purposes.
    /// </summary>
    //[AutoNetworkedField]
    public Vector2 Location;

    /// <summary>
    /// A list of nodes which this node flows into.
    /// </summary>
    //[AutoNetworkedField]
    [DataField]
    public List<NetEntity> NextNodes = new();

    /// <summary>
    /// A dictionary that holds the lengths of each segment. The NetEntity is the NextNodes entity the segment connects to.
    /// </summary>
    public Dictionary<NetEntity, float> SegmentControlPointMultiplier = new();

    /// <summary>
    /// A list of nodes which flow into this node.
    /// </summary>
    //[AutoNetworkedField]
    [DataField]
    public List<NetEntity> PriorNodes = new();

    /// <summary>
    /// The direction in which the river flows.
    /// </summary>
    [DataField/*, AutoNetworkedField*/]
    public Angle FlowDirection = 0d;

    /// <summary>
    /// The distance from the Node that Shuttles are influenced by its effects.
    /// </summary>
    [DataField/*, AutoNetworkedField*/]
    public float NodeRange = 40.0f; //TODO: Figure out a reasonable value

    /// <summary>
    /// Whether or not to allow systems to change the flow direction of this Node.
    /// </summary>
    [DataField]
    public bool LockFlowDirection = false;
}

[Serializable, NetSerializable]
public sealed class RiverNodeComponentState : ComponentState
{
    public Vector2 Location;
    public List<NetEntity> NextNodes;
    public Dictionary<NetEntity, float> SegmentControlPointMultiplier;
    public List<NetEntity> PriorNodes;
    public Angle FlowDirection;
    public float NodeRange;
    public bool LockFlowDirection;

    public RiverNodeComponentState(Vector2 location, List<NetEntity> nextNodes, Dictionary<NetEntity, float> segmentControlPointMultiplier, List<NetEntity> priorNodes, Angle flowDirection, float nodeRange, bool lockFlowDirection)
    {
        Location = location;
        NextNodes = nextNodes;
        SegmentControlPointMultiplier = segmentControlPointMultiplier;
        PriorNodes = priorNodes;
        FlowDirection = flowDirection;
        NodeRange = nodeRange;
        LockFlowDirection = lockFlowDirection;
    }
}
