using Robust.Shared.GameStates;

namespace Content.Shared._NF.Anomaly;

/// <summary>
/// This is used for tracking anomalies which have ended up off grid, to periodically check whether they should be timed out.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class AnomalyLinkExpiryComponent : Component
{
    /// <summary>
    /// The time at which the link should check if it should be broken.
    /// </summary>
    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan EndTime;


    /// <summary>
    /// How often in seconds the component checks to see if the link should expire.
    /// If the EntityQuery seems a bit unperformant this can be increased.
    /// </summary>
    [DataField]
    public TimeSpan CheckFrequency = TimeSpan.FromSeconds(10);

    /// <summary>
    /// How far away the vessel is allowed to be from the anomaly its linked to in metres if they don't share a grid
    /// </summary>
    [DataField]
    public float MaxDistance = 50;

}
