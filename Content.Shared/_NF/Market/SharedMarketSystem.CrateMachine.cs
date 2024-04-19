using Robust.Shared.Serialization;

namespace Content.Shared._NF.Market;

public sealed class MarketSystem: SharedMarketSystem
{

    [Serializable, NetSerializable]
    protected sealed class CrateMachineComponentState : ComponentState
    {
        public bool Powered;
        public bool Engaged;

        public CrateMachineComponentState(bool powered, bool engaged)
        {
            Powered = powered;
            Engaged = engaged;
        }
    }
}
