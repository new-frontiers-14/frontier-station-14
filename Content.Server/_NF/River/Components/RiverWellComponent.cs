using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server._NF.River.Components;

[RegisterComponent]
public sealed partial class RiverWellComponent : Component
{
    [DataField]
    public float Radius = 200f;
    [DataField]
    public Chirality FlowRotation = Chirality.Clockwise;

    public List<EntityUid> WellNodes = new();
    public List<EntityUid> ConnectedWells = new();
    /// <summary>
    /// The EntityUid Key is a well EntityUid, and the ArrivalDeparturePair are the WellNodes which indicate the arrival and departure point for rivers connecting the other well.
    /// </summary>
    public Dictionary<EntityUid, ArrivalDeparturePair> Connections = new();
}

public struct ArrivalDeparturePair
{
    public ArrivalDeparturePair(EntityUid arrivalNode, EntityUid departureNode)
    {
        ArrivalNode = arrivalNode;
        DepartureNode = departureNode;
    }
    public EntityUid ArrivalNode { get; set; }
    public EntityUid DepartureNode { get; set; }

    public override string ToString() => $"({ArrivalNode}, {DepartureNode})";
}

public enum Chirality
{
    Clockwise,
    Counterclockwise
}
