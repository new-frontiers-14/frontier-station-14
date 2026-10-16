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
    public List<NetEntity> NextNodes = new();

    /// <summary>
    /// A list of nodes which flow into this node.
    /// </summary>
    //[AutoNetworkedField]
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
    public float NodeRange = 50.0f; //TODO: Figure out a reasonable value

    /// <summary>
    /// Influences whether rivers are keen to merge or split. Higher numbers wish to split rather than merge.
    /// Lower numbers wish to merge rather than split.
    /// </summary>
    public float Saturation = 1f;

    //[AutoNetworkedField]
    public Vector2 ForwardControlPoint;

}

[Serializable, NetSerializable]
public sealed class RiverNodeComponentState : ComponentState
{
    public Vector2 Location;
    public List<NetEntity> NextNodes;
    public List<NetEntity> PriorNodes;
    public Angle FlowDirection;
    public float NodeRange;
    public float Saturation;
    public Vector2 ForwardControlPoint;

    public RiverNodeComponentState(Vector2 location, List<NetEntity> nextNodes, List<NetEntity> priorNodes, Angle flowDirection, float nodeRange, float saturation, Vector2 forwardControlPoint)
    {
        Location = location;
        NextNodes = nextNodes;
        PriorNodes = priorNodes;
        FlowDirection = flowDirection;
        NodeRange = nodeRange;
        Saturation = saturation;
        ForwardControlPoint = forwardControlPoint;
    }
}
