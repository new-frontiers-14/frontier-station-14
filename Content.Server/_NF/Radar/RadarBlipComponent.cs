namespace Content.Server._NF.Radar;

using Content.Shared._NF.Radar;

/// <summary>
/// These handle objects which should be represented by radar blips.
/// </summary>
[RegisterComponent]
public sealed partial class RadarBlipComponent : Component
{
    /// <summary>
    /// Color that gets shown on the radar screen.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("radarColor")]
    private Color _radarColor = Color.Red;
    public Color RadarColor { get => _radarColor; set => _radarColor = value; }

    /// <summary>
    /// Color that gets shown on the radar screen when the blip is highlighted.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("highlightedRadarColor")]
    private Color _highlightedRadarColor = Color.OrangeRed;
    public Color HighlightedRadarColor { get => _highlightedRadarColor; set => _highlightedRadarColor = value; }

    /// <summary>
    /// Scale of the blip.
    /// </summary>
    [DataField]
    private float _scale = 1;
    public float Scale { get => _scale; set => _scale = value; }

    /// <summary>
    /// The shape of the blip on the radar.
    /// </summary>
    [DataField]
    private RadarBlipShape _shape = RadarBlipShape.Circle;
    public RadarBlipShape Shape { get => _shape; set => _shape = value; }

    /// <summary>
    /// Whether this blip should be shown even when parented to a grid.
    /// </summary>
    [DataField]
    private bool _requireNoGrid = false;
    public bool RequireNoGrid { get => _requireNoGrid; set => _requireNoGrid = value; }

    /// <summary>
    /// Whether this blip should be visible on radar across different grids.
    /// </summary>
    [DataField]
    private bool _visibleFromOtherGrids = false;
    public bool VisibleFromOtherGrids { get => _visibleFromOtherGrids; set => _visibleFromOtherGrids = value; }

    /// <summary>
    /// Whether this blip is enabled and should be shown on radar.
    /// </summary>
    [DataField]
    private bool _enabled = true;
    public bool Enabled { get => _enabled; set => _enabled = value; }
}
