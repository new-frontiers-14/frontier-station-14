namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(NfAdventureRuleSystem))]
public sealed partial class AdventureRuleComponent : Component
{
    public readonly List<EntityUid> NFPlayerMinds = new();
    public readonly List<EntityUid> CargoDepots = new();
    public readonly List<EntityUid> MarketStations = new();
    public readonly List<EntityUid> RequiredPOIs = new();
    public readonly List<EntityUid> OptionalPOIs = new();
    public readonly List<EntityUid> UniquePOIs = new();
}
