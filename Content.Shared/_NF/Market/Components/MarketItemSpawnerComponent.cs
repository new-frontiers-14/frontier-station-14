using Robust.Shared.GameStates;

namespace Content.Shared._NF.Market.Components;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class MarketItemSpawnerComponent : Component
{

    [NonSerialized]
    public List<MarketData> ItemsToSpawn;
}
