using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server._NF.ShuttleMaker.Components;
[RegisterComponent]
public sealed partial class ShuttleMakerComponent : Component
{
    //The function of this component is to indicate what anchorable entities may define something as being a Shuttle or Ship.
    //Add this component components that, while they are anchored to a grid,  will ensure the grid has the components needed for Ship/Shuttle functionality.
    //A natural example of what contains this component are Shuttle Consoles.
    //(Note, this does not currently interact with the ShuttleComponent, as all grids naturally have a ShuttleComponent)
    public EntityUid? ShuttleGrid;
}
