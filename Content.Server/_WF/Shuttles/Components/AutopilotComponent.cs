using Robust.Shared.Map;

namespace Content.Server._WF.Shuttles.Components;

/// <summary>
/// Enables automatic navigation for a shuttle to a target destination.
/// </summary>
[RegisterComponent]
public sealed partial class AutopilotComponent : Component
{
    /// <summary>
    /// Whether autopilot is currently enabled.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled = false;

    /// <summary>
    /// The target coordinates to navigate to.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public MapCoordinates? TargetCoordinates;

    /// <summary>
    /// Speed multiplier when autopilot is active (60% of max speed).
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float SpeedMultiplier = 0.6f;

    /// <summary>
    /// Distance at which autopilot will automatically disable (in meters).
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ArrivalDistance = 200f;

    /// <summary>
    /// Distance to start slowing down (in meters).
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float SlowdownDistance = 300f;

    /// <summary>
    /// Minimum distance to consider an obstacle (in meters).
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ObstacleAvoidanceDistance = 75f;

    /// <summary>
    /// Maximum range to scan for obstacles (in meters).
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ScanRange = 125f;
}
