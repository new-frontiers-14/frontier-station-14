using Content.Shared._NF.Radar;

namespace Content.Server._NF.Radar;

/// <summary>
/// Handles objects which should be represented by radar blips.
/// </summary>
[RegisterComponent]
public sealed partial class RadarBlipComponent : Component
{
    /// <summary>
    /// Color that gets shown on the radar screen.
    /// </summary>
    [ViewVariables, DataField]
    public Color RadarColor { get; set; } = Color.Red;

    /// <summary>
    /// Color that gets shown on the radar screen when the blip is highlighted.
    /// </summary>
    [ViewVariables, DataField]
    public Color HighlightedRadarColor { get; set; } = Color.OrangeRed;

    /// <summary>
    /// Scale of the blip.
    /// </summary>
    [ViewVariables, DataField]
    public float Scale { get; set; } = 1f;

    /// <summary>
    /// The shape of the blip on the radar.
    /// </summary>
    [ViewVariables, DataField]
    public RadarBlipShape Shape { get; set; } = RadarBlipShape.Circle;

    /// <summary>
    /// Whether this blip should be shown even when parented to a grid.
    /// </summary>
    [ViewVariables, DataField]
    public bool RequireNoGrid { get; set; } = false;

    /// <summary>
    /// Whether this blip should be visible on radar across different grids.
    /// </summary>
    [ViewVariables, DataField]
    public bool VisibleFromOtherGrids { get; set; } = false;

    /// <summary>
    /// Whether this blip is enabled and should be shown on radar.
    /// </summary>
    [ViewVariables, DataField]
    public bool Enabled { get; set; } = true;
}
