using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Content.Shared.Shuttles.Components; // Frontier

namespace Content.Shared._NF.Atmos.BUIStates;

/// <summary>
/// State of each individual docking port for interface purposes
/// </summary>
[Serializable, NetSerializable]
public sealed class GaslockPortState
{
    public string Name = string.Empty;

    public NetCoordinates Coordinates;
    public Angle Angle;
    public NetEntity Entity;
    public bool Connected => GridDockedWith != null;

    public NetEntity? GridDockedWith;

    public float Pressure;
    public bool Inwards;
    public bool Enabled;

    public string? LabelName;
    public Color RadarColor;
    public Color HighlightedRadarColor;
    public bool ReceiveOnly;
    public DockType DockType;
}
