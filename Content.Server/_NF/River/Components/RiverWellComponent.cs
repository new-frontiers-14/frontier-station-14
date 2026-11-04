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
}

public enum Chirality
{
    Clockwise,
    Counterclockwise
}
