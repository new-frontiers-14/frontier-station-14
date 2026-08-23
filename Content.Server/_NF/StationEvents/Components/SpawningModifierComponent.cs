namespace Content.Server._NF.StationEvents.Components;

// A component to modify the bounds that objects spawn in. Apply this to targets around which things should spawn.
[RegisterComponent]
public sealed partial class SpawningModifierComponent : Component
{
    /// <summary>
    /// The type of this bias target. Should match between this component and whatever you want to spawn's bluespaceerrorrulecomponent.
    /// </summary>
    [DataField(required: true)]
    public string id = "";

    /// <summary>
    /// Minimum distance to spawn away from the target.
    /// </summary>
    [DataField]
    public float MinimumDistance = 500f;

    /// <summary>
    /// Maximum distance to spawn away from the target.
    /// </summary>
    [DataField]
    public float MaximumDistance = 1000f;

    /// <summary>
    /// Minimum angle to spawn relative to the target, in degrees. 0 degrees is a vector that points away from 0,0 originating at the target.
    /// </summary>
    [DataField]
    public float MinimumAngle = 0f;

    /// <summary>
    /// Minimum angle to spawn relative to the target, in degrees. 0 degrees is a vector that points away from 0,0 originating at the target.
    /// </summary>
    [DataField]
    public float MaximumAngle = 360f;

}