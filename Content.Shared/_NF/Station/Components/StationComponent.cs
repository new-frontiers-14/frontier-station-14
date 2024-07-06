
using Robust.Shared.GameStates;

namespace Content.Shared._NF.Station.Components
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class StationComponent :Component
    {
        [ViewVariables]
        public bool Enabled = true;

    }
}
