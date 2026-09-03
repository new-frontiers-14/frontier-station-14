using Robust.Shared.GameStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._NF.River.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class /*Shared*/RiverNodeComponent : Component
{
    [AutoNetworkedField]
    public Vector2 Location;
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
    public float Boost = 0.3f;

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
}
