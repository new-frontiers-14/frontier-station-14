using Content.Server._NF.Market.Systems;
using Content.Shared._NF.Market;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;

namespace Content.Server._NF.Market.Components;

/// <summary>
/// Component that belongs to any dynamically linked
/// </summary>
[RegisterComponent]
[Access(typeof(MarketSystem))]
public sealed partial class DynamicMarketComponent : Component
{
    public float DefaultMarketModifier;

    public float CurrentMarketModifier;

    [DataField]
    public TimeSpan UpdateFrequency = TimeSpan.FromSeconds(600);

    public TimeSpan TimeToNextUpdate = TimeSpan.Zero;
}
