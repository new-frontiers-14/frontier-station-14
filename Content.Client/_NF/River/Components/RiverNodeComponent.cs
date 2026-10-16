using Content.Shared._NF.River.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client._NF.River.Components;

[RegisterComponent]
public sealed partial class RiverNodeComponent : SharedRiverNodeComponent
{
    /// <summary>
    /// The mesh of the river segments that spring from this node. For Mass Scanner rendering.
    /// </summary>
    public Vector2[] SegmentMesh;

    public readonly Dictionary<EntityUid, Box2> BoundingBoxes = new();
}

