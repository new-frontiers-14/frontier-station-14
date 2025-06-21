using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Emberfall.Weapons.Ranged;

/// <summary>
/// Added to projectiles to give them tracer effects
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TracerComponent : Component
{
    /// <summary>
    /// How long the tracer effect should remain visible for after firing
    /// </summary>
    [DataField]
    public float Lifetime = 10f;

    /// <summary>
    /// The maximum length of the tracer trail
    /// </summary>
    [DataField]
    public float Length = 2f;

    /// <summary>
    /// Color of the tracer line effect
    /// </summary>
    [DataField]
    public Color Color = Color.Red;

    [ViewVariables]
    public TracerData Data = default!;
}

[Serializable, NetSerializable, DataRecord]
public struct TracerData(List<Vector2> positionHistory, TimeSpan endTime)
{
    /// <summary>
    /// The history of positions this tracer has moved through
    /// </summary>
    public List<Vector2> PositionHistory = positionHistory;

    /// <summary>
    /// When this tracer effect should end
    /// </summary>
    public TimeSpan EndTime = endTime;
}
