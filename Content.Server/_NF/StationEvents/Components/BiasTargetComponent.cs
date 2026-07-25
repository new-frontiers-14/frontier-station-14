namespace Content.Server._NF.StationEvents.Components;

// A component to bias the direction that objects spawn in.
// Only the direction relative to 0,0 is considered
[RegisterComponent]
public sealed partial class BiasTargetComponent : Component
{
    /// <summary>
    /// The type of this bias target. Should match between this component and whatever you want to spawn's bluespaceerrorrulecomponent.
    /// </summary>
    [DataField(required: true)]
    public string id = "";
}