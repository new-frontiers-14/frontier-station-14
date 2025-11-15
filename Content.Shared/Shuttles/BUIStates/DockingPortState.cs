using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Content.Shared.Shuttles.Components; // Frontier

namespace Content.Shared.Shuttles.BUIStates;

/// <summary>
/// State of each individual docking port for interface purposes
/// </summary>
[Serializable, NetSerializable]
public sealed class DockingPortState
{
    public string Name = string.Empty;

    public NetCoordinates Coordinates;
    public Angle Angle;
    public NetEntity Entity;
    public bool Connected => GridDockedWith != null;

    public NetEntity? GridDockedWith;

<<<<<<< HEAD
    // Frontier: label, colors, type, receive only
    public string? LabelName;
    public Color RadarColor;
    public Color HighlightedRadarColor;
    public bool ReceiveOnly;
    public DockType DockType;
    // End Frontier
=======
    /// <summary>
    /// The default colour used to shade a dock on a radar screen
    /// </summary>
    public Color Color;

    /// <summary>
    /// The colour used to shade a dock on a radar screen if it is highlighted (hovered over/selected on docking screen/shown in the main ship radar)
    /// </summary>
    public Color HighlightedColor;
>>>>>>> e917c8e067e70fa369bf8f1f393a465dc51caee8
}
