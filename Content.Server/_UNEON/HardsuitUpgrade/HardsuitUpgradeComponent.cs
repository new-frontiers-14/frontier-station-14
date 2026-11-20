using Content.Shared._UNEON.HardsuitUpgrade;

namespace Content.Server._UNEON.HardsuitUpgrade;

[RegisterComponent]
public sealed partial class HardsuitUpgradeComponent : Component
{
    // Save paid work with the station, including remaining powered processing time.
    [DataField] public HardsuitUpgradeKind? Job;
    [DataField] public EntityUid? JobSuit;
    [DataField] public float Target;
    [DataField] public float Remaining;
    [DataField] public float Duration;
    [DataField] public Dictionary<string, int> PaidMaterials = new();
}
