using System.Linq;
using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared._NF.Radar;

/// <summary>
/// The shape of the radar blip.
/// </summary>
[Serializable, NetSerializable]
public enum RadarBlipShape
{
    /// <summary>Circle shape.</summary>
    Circle,
    /// <summary>Square shape.</summary>
    Square,
    /// <summary>Triangle shape.</summary>
    Triangle,
    /// <summary>Star shape.</summary>
    Star,
    /// <summary>Diamond shape.</summary>
    Diamond,
    /// <summary>Hexagon shape.</summary>
    Hexagon,
    /// <summary>Arrow shape.</summary>
    Arrow,
    /// <summary>Ring shape.</summary>
    Ring
}

[Serializable, NetSerializable]
public sealed class GiveBlipsEvent : EntityEventArgs
{
    /// <summary>
    /// Blips are now (grid entity, position, scale, color, shape).
    /// If grid entity is null, position is in world coordinates.
    /// If grid entity is not null, position is in grid-local coordinates.
    /// </summary>
    public readonly List<(NetEntity? Grid, Vector2 Position, float Scale, Color Color, RadarBlipShape Shape)> Blips;

    // Constructor for back-compatibility
    public GiveBlipsEvent(List<(Vector2, float, Color)> blips)
    {
        Blips = blips.Select(b => ((NetEntity?)null, b.Item1, b.Item2, b.Item3, RadarBlipShape.Circle)).ToList();
    }

    public GiveBlipsEvent(List<(NetEntity? Grid, Vector2 Position, float Scale, Color Color, RadarBlipShape Shape)> blips)
    {
        Blips = blips;
    }
}

[Serializable, NetSerializable]
public sealed class RequestBlipsEvent : EntityEventArgs
{
    public NetEntity Radar;
    public RequestBlipsEvent(NetEntity radar)
    {
        Radar = radar;
    }
}
