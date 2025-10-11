using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Content.Shared._NF.Shuttles.Events;
using Content.Shared.Shuttles.Components; // Frontier
using System.Numerics; // Frontier - InertiaDampeningMode access

namespace Content.Shared.Shuttles.BUIStates;

[Serializable, NetSerializable]
public sealed class NavInterfaceState
{
    public float MaxRange;

    /// <summary>
    /// The relevant coordinates to base the radar around.
    /// </summary>
    public NetCoordinates? Coordinates;

    /// <summary>
    /// The relevant rotation to rotate the angle around.
    /// </summary>
    public Angle? Angle;

    public Dictionary<NetEntity, List<DockingPortState>> Docks;

    public bool RotateWithEntity = true;

    // Frontier fields
    /// <summary>
    /// Frontier - the state of the shuttle's inertial dampeners
    /// </summary>
    public InertiaDampeningMode DampeningMode;

    /// <summary>
    /// Frontier: settable maximum IFF range
    /// </summary>
    public float? MaxIffRange = null;

    /// <summary>
    /// Frontier: settable coordinate visibility
    /// </summary>
    public bool HideCoords = false;

    /// <summary>
    /// Service Flags
    /// </summary>
    public ServiceFlags ServiceFlags { get; set; }

    /// <summary>
    /// A settable target to show on radar
    /// </summary>
    public Vector2? Target { get; set; }

    /// <summary>
    /// A settable target to show on radar
    /// </summary>
    public NetEntity? TargetEntity { get; set; }

    /// <summary>
    /// Frontier: whether or not to show the target coords
    /// </summary>
    public bool HideTarget = true;
    // End Frontier fields
    public NavInterfaceState(
        float maxRange,
        NetCoordinates? coordinates,
        Angle? angle,
        Dictionary<NetEntity, List<DockingPortState>> docks,
        InertiaDampeningMode dampeningMode, // Frontier
        ServiceFlags serviceFlags, // Frontier
        Vector2? target, // Frontier
        NetEntity? targetEntity, // Frontier
        bool hideTarget) // Frontier
    {
        MaxRange = maxRange;
        Coordinates = coordinates;
        Angle = angle;
        Docks = docks;
        DampeningMode = dampeningMode; // Frontier
        ServiceFlags = serviceFlags; // Frontier
        Target = target; // Frontier
        TargetEntity = targetEntity; // Frontier
        HideTarget = hideTarget; // Frontier
    }
}

[Serializable, NetSerializable]
public enum RadarConsoleUiKey : byte
{
    Key
}
