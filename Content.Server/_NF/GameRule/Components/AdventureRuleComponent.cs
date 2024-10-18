namespace Content.Server._NF.GameRule.Components;

[RegisterComponent, Access(typeof(NfAdventureRuleSystem))]
public sealed partial class AdventureRuleComponent : Component
{
    public List<EntityUid> NFPlayerMinds = new();
    public List<EntityUid> CargoDepots = new();
    public List<EntityUid> MarketStations = new();
    public List<EntityUid> RequiredPois = new();
    public List<EntityUid> OptionalPois = new();
    public List<EntityUid> UniquePois = new();
}
