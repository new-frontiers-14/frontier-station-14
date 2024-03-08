using System.Numerics;
using Content.Client.Resources;
using Content.Client.UserInterface.Controls;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Collections;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Utility;

namespace Content.Client.Shuttles.UI;

/// <summary>
/// Displays nearby grids inside of a control.
/// </summary>
public sealed class RadarControl : MapGridControl
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
    private readonly SharedTransformSystem _transform;

    private const float GridLinesDistance = 32f;

    private const int RadarBlipSize = 15;
    private const int RadarFontSize = 10;

    /// <summary>
    /// Used to transform all of the radar objects. Typically is a shuttle console parented to a grid.
    /// </summary>
    private EntityCoordinates? _coordinates;

    private Angle? _rotation;

    /// <summary>
    /// Shows a label on each radar object.
    /// </summary>
    private readonly Dictionary<EntityUid, Control> _iffControls = new();

    private readonly Dictionary<EntityUid, List<DockingInterfaceState>> _docks = new();

    public bool ShowIFF { get; set; } = true;
    public bool ShowIFFShuttles { get; set; } = true;
    public bool ShowDocks { get; set; } = true;

    /// <summary>
    ///   If present, called for every IFF. Must determine if it should or should not be shown.
    /// </summary>
    public Func<EntityUid, MapGridComponent, IFFComponent?, bool>? IFFFilter { get; set; } = null;

    /// <summary>
    /// Currently hovered docked to show on the map.
    /// </summary>
    public NetEntity? HighlightedDock;

    /// <summary>
    /// Raised if the user left-clicks on the radar control with the relevant entitycoordinates.
    /// </summary>
    public Action<EntityCoordinates>? OnRadarClick;

    private List<Entity<MapGridComponent>> _grids = new();

    public RadarControl() : base(64f, 256f, 256f)
    {
        _transform = _entManager.System<SharedTransformSystem>();
        _resourceCache = IoCManager.Resolve<IResourceCache>();
    }

    public void SetMatrix(EntityCoordinates? coordinates, Angle? angle)
    {
        _coordinates = coordinates;
        _rotation = angle;
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);

        if (_coordinates == null || _rotation == null || args.Function != EngineKeyFunctions.UIClick ||
            OnRadarClick == null)
        {
            return;
        }

        var a = InverseScalePosition(args.RelativePosition);
        var relativeWorldPos = a with { Y = -a.Y };
        relativeWorldPos = _rotation.Value.RotateVec(relativeWorldPos);
        var coords = _coordinates.Value.Offset(relativeWorldPos);
        OnRadarClick?.Invoke(coords);
    }

    /// <summary>
    /// Gets the entity coordinates of where the mouse position is, relative to the control.
    /// </summary>
    [PublicAPI]
    public EntityCoordinates GetMouseCoordinatesFromCenter()
    {
        if (_coordinates == null || _rotation == null)
        {
            return EntityCoordinates.Invalid;
        }

        var pos = _uiManager.MousePositionScaled.Position - GlobalPosition;
        var relativeWorldPos = _rotation.Value.RotateVec(pos);

        // I am not sure why the resulting point is 20 units under the mouse.
        return _coordinates.Value.Offset(relativeWorldPos);
    }

    public void UpdateState(RadarConsoleBoundInterfaceState ls)
    {
        WorldMaxRange = ls.MaxRange;

        if (WorldMaxRange < WorldRange)
        {
            ActualRadarRange = WorldMaxRange;
        }

        if (WorldMaxRange < WorldMinRange)
            WorldMinRange = WorldMaxRange;

        ActualRadarRange = Math.Clamp(ActualRadarRange, WorldMinRange, WorldMaxRange);

        _docks.Clear();

        // This draws the purple dots where docking airlocks are located.
        foreach (var state in ls.Docks)
        {
            var coordinates = state.Coordinates;
            var grid = _docks.GetOrNew(_entManager.GetEntity(coordinates.NetEntity));
            grid.Add(state);
        }
    }

    public class BlipData
    {
        public bool IsOutsideRadarCircle { get; set; }
        public Vector2 UiPosition { get; set; }
        public Vector2 VectorToPosition { get; set; }
        public Color Color { get; set; }
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        handle.DrawCircle(new Vector2(MidPoint, MidPoint), ScaledMinimapRadius, Color.Black);

        // No data
        if (_coordinates == null || _rotation == null)
        {
            Clear();
            return;
        }

        var gridLines = new Color(0.08f, 0.08f, 0.08f);
        var gridLinesRadial = 8;
        var gridLinesEquatorial = (int) Math.Floor(WorldRange / GridLinesDistance);

        for (var i = 1; i < gridLinesEquatorial + 1; i++)
        {
            handle.DrawCircle(new Vector2(MidPoint, MidPoint), GridLinesDistance * MinimapScale * i, gridLines, false);
        }

        for (var i = 0; i < gridLinesRadial; i++)
        {
            Angle angle = (Math.PI / gridLinesRadial) * i;
            var aExtent = angle.ToVec() * ScaledMinimapRadius;
            handle.DrawLine(new Vector2(MidPoint, MidPoint) - aExtent, new Vector2(MidPoint, MidPoint) + aExtent, gridLines);
        }

        var metaQuery = _entManager.GetEntityQuery<MetaDataComponent>();
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
        var fixturesQuery = _entManager.GetEntityQuery<FixturesComponent>();
        var bodyQuery = _entManager.GetEntityQuery<PhysicsComponent>();

        if (!xformQuery.TryGetComponent(_coordinates.Value.EntityId, out var xform)
            || xform.MapID == MapId.Nullspace)
        {
            Clear();
            return;
        }

        var (pos, rot) = _transform.GetWorldPositionRotation(xform);
        var offset = _coordinates.Value.Position;
        var offsetMatrix = Matrix3.CreateInverseTransform(pos, rot + _rotation.Value);

        var shown = new HashSet<EntityUid>();

        // Draw our grid in detail
        var ourGridId = xform.GridUid;
        if (_entManager.TryGetComponent<MapGridComponent>(ourGridId, out var ourGrid) &&
            fixturesQuery.HasComponent(ourGridId.Value))
        {
            var ourGridMatrix = _transform.GetWorldMatrix(ourGridId.Value);
            Matrix3.Multiply(in ourGridMatrix, in offsetMatrix, out var matrix);

            DrawGrid(handle, matrix, ourGrid, Color.MediumSpringGreen, true);
            DrawDocks(handle, ourGridId.Value, matrix, shown);
        }

        var invertedPosition = _coordinates.Value.Position - offset;
        invertedPosition.Y = -invertedPosition.Y;
        // Don't need to transform the InvWorldMatrix again as it's already offset to its position.

        // Draw radar position on the station
        handle.DrawCircle(ScalePosition(invertedPosition), 5f, Color.Lime);


        _grids.Clear();
        _mapManager.FindGridsIntersecting(xform.MapID, new Box2(pos - MaxRadarRangeVector, pos + MaxRadarRangeVector), ref _grids, approx: true, includeMap: false);

        // Frontier - collect blip location data outside foreach - more changes ahead
        var blipDataList = new List<BlipData>();

        // Draw other grids... differently
        foreach (var grid in _grids)
        {
            var gUid = grid.Owner;
            if (gUid == ourGridId || !fixturesQuery.HasComponent(gUid))
                continue;

            var gridBody = bodyQuery.GetComponent(gUid);
            if (gridBody.Mass < 10f)
            {
                ClearLabel(gUid);
                continue;
            }

            _entManager.TryGetComponent<IFFComponent>(gUid, out var iff);

            var hideShuttleLabels = iff != null && (iff.Flags & IFFFlags.Hide) != 0x0;
            if (hideShuttleLabels)
            {
                continue;
            }

            shown.Add(gUid);
            var name = metaQuery.GetComponent(gUid).EntityName;
            if (name == string.Empty)
            {
                name = Loc.GetString("shuttle-console-unknown");
            }

            var gridMatrix = _transform.GetWorldMatrix(gUid);
            Matrix3.Multiply(in gridMatrix, in offsetMatrix, out var matty);
            var color = iff?.Color ?? Color.Gold;

            // Others default:
            // Color.FromHex("#FFC000FF")
            // Hostile default: Color.Firebrick

            /****************************************************************************
             * FRONTIER - BEGIN radar improvements
             * Everything below until end block belong to frontier improvements to radar
             *****************************************************************************/

            if (ShowIFF && (iff == null && IFFComponent.ShowIFFDefault || (iff.Flags & IFFFlags.HideLabel) == 0x0))
            {
                var gridBounds = grid.Comp.LocalAABB;
                var gridCentre = matty.Transform(gridBody.LocalCenter);
                gridCentre.Y = -gridCentre.Y;
                var distance = gridCentre.Length();

                // y-offset the control to always render below the grid (vertically)
                var yOffset = Math.Max(gridBounds.Height, gridBounds.Width) * MinimapScale / 1.8f / UIScale;

                // The actual position in the UI. We offset the matrix position to render it off by half its width
                // plus by the offset.
                var uiPosition = ScalePosition(gridCentre) / UIScale;

                // Confines the UI position within the viewport.
                var uiXCentre = (int) Width / 2;
                var uiYCentre = (int) Height / 2;
                var uiXOffset = uiPosition.X - uiXCentre;
                var uiYOffset = uiPosition.Y - uiYCentre;
                var uiDistance = (int) Math.Sqrt(Math.Pow(uiXOffset, 2) + Math.Pow(uiYOffset, 2));
                var uiX = uiXCentre * uiXOffset / uiDistance;
                var uiY = uiYCentre * uiYOffset / uiDistance;

                var isOutsideRadarCircle = uiDistance > Math.Abs(uiX) && uiDistance > Math.Abs(uiY);
                if (isOutsideRadarCircle)
                {
                    // 0.95f for offsetting the icons slightly away from edge of radar so it doesnt clip.
                    uiX = uiXCentre * uiXOffset / uiDistance * 0.95f;
                    uiY = uiYCentre * uiYOffset / uiDistance * 0.95f;
                    uiPosition = new Vector2(
                        x: uiX + uiXCentre,
                        y: uiY + uiYCentre
                    );
                }

                var scaledMousePosition = GetMouseCoordinatesFromCenter().Position * UIScale;
                var isMouseOver = Vector2.Distance(scaledMousePosition, uiPosition * UIScale) < 30f;

                // Distant stations that are not player controlled ships
                var isDistantPOI = iff != null || (iff == null || (iff.Flags & IFFFlags.IsPlayerShuttle) == 0x0);

                if (!isOutsideRadarCircle || isDistantPOI || isMouseOver)
                {
                    Label label;

                    if (!_iffControls.TryGetValue(gUid, out var control))
                    {
                        label = new Label
                        {
                            HorizontalAlignment = HAlignment.Left,
                        };

                        _iffControls[gUid] = label;
                        AddChild(label);
                    }
                    else
                    {
                        label = (Label) control;
                    }

                    label.FontColorOverride = color;
                    label.FontOverride = _resourceCache.GetFont("/Fonts/NotoSans/NotoSans-Regular.ttf", RadarFontSize);
                    label.Visible = ShowIFFShuttles || iff == null || (iff.Flags & IFFFlags.IsPlayerShuttle) == 0x0 || isMouseOver;
                    if (IFFFilter != null)
                    {
                        label.Visible &= IFFFilter(gUid, grid.Comp, iff);
                    }

                    // Shows decimal when distance is < 50m, otherwise pointless to show it.
                    var displayedDistance = distance < 50f ? $"{distance:0.0}" : distance < 1000 ? $"{distance:0}" : $"{distance / 1000:0.0}k";
                    label.Text = Loc.GetString("shuttle-console-iff-label", ("name", name), ("distance", displayedDistance));

                    var sideCorrection = isOutsideRadarCircle && uiPosition.X > Width / 2 ? -label.Size.X -20 : 0;
                    var blipCorrection = (RadarBlipSize * 0.7f);
                    var correctedUiPosition = uiPosition with
                    {
                        X = uiPosition.X > Width / 2
                            ? uiPosition.X + blipCorrection + sideCorrection
                            : uiPosition.X + blipCorrection,
                        Y = uiPosition.Y - 10 // Wanted to use half the label height, but this makes text jump when visibility changes.
                    };

                    LayoutContainer.SetPosition(label, correctedUiPosition);
                }
                else
                {
                    ClearLabel(gUid);
                }

                blipDataList.Add(new BlipData
                {
                    IsOutsideRadarCircle = isOutsideRadarCircle,
                    UiPosition = uiPosition,
                    VectorToPosition = uiPosition - new Vector2(uiXCentre, uiYCentre),
                    Color = color
                });
            }
            else
            {
                ClearLabel(gUid);
            }

            DrawBlips(handle, blipDataList);

            // Detailed view
            DrawGrid(handle, matty, grid, color, true);

            DrawDocks(handle, gUid, matty, shown);
        }

        foreach (var (ent, _) in _iffControls)
        {
            if (shown.Contains(ent))
            {
                continue;
            }
            ClearLabel(ent);
        }
    }

    /**
     * Frontier - Adds blip style triangles that are on ships or pointing towards ships on the edges of the radar.
     * Draws blips at the BlipData's uiPosition and uses VectorToPosition to rotate to point towards ships.
     */
    private void DrawBlips(
        DrawingHandleBase handle,
        List<BlipData> blipDataList
    )
    {
        var blipValueList = new Dictionary<Color, ValueList<Vector2>>();

        foreach (var blipData in blipDataList)
        {
            var triangleShapeVectorPoints = new[]
            {
                new Vector2(0, 0),
                new Vector2(RadarBlipSize, 0),
                new Vector2(RadarBlipSize * 0.5f, RadarBlipSize)
            };

            if (blipData.IsOutsideRadarCircle)
            {
                // Calculate the angle of rotation
                var angle = (float) Math.Atan2(blipData.VectorToPosition.Y, blipData.VectorToPosition.X) + -1.6f;

                // Manually create a rotation matrix
                var cos = (float) Math.Cos(angle);
                var sin = (float) Math.Sin(angle);
                float[,] rotationMatrix = { { cos, -sin }, { sin, cos } };

                // Rotate each vertex
                for (var i = 0; i < triangleShapeVectorPoints.Length; i++)
                {
                    var vertex = triangleShapeVectorPoints[i];
                    var x = vertex.X * rotationMatrix[0, 0] + vertex.Y * rotationMatrix[0, 1];
                    var y = vertex.X * rotationMatrix[1, 0] + vertex.Y * rotationMatrix[1, 1];
                    triangleShapeVectorPoints[i] = new Vector2(x, y);
                }
            }

            var triangleCenterVector =
                (triangleShapeVectorPoints[0] + triangleShapeVectorPoints[1] + triangleShapeVectorPoints[2]) / 3;

            // Calculate the vectors from the center to each vertex
            var vectorsFromCenter = new Vector2[3];
            for (int i = 0; i < 3; i++)
            {
                vectorsFromCenter[i] = (triangleShapeVectorPoints[i] - triangleCenterVector) * UIScale;
            }

            // Calculate the vertices of the new triangle
            var newVerts = new Vector2[3];
            for (var i = 0; i < 3; i++)
            {
                newVerts[i] = (blipData.UiPosition * UIScale) + vectorsFromCenter[i];
            }

            if (!blipValueList.TryGetValue(blipData.Color, out var valueList))
            {
                valueList = new ValueList<Vector2>();

            }
            valueList.Add(newVerts[0]);
            valueList.Add(newVerts[1]);
            valueList.Add(newVerts[2]);
            blipValueList[blipData.Color] = valueList;
        }

        // One draw call for every color we have
        foreach (var color in blipValueList)
        {
            handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, color.Value.Span, color.Key);
        }
    }
    /****************************************************************************
     * FRONTIER - END radar improvements
     *****************************************************************************/

    private void Clear()
    {
        foreach (var (_, label) in _iffControls)
        {
            label.Dispose();
        }

        _iffControls.Clear();
    }

    private void ClearLabel(EntityUid uid)
    {
        if (!_iffControls.TryGetValue(uid, out var label))
        {
            return;
        }
        label.Dispose();
        _iffControls.Remove(uid);
    }

    private void DrawDocks(DrawingHandleScreen handle, EntityUid uid, Matrix3 matrix, HashSet<EntityUid> shown)
    {
        if (!ShowDocks)
            return;

        const float dockScale = 1f;

        if (_docks.TryGetValue(uid, out var docks))
        {
            // Keep track of which logical docks we've already drawn labels on, to prevent
            // duplicating labels for each group of docks.
            var labeled = new HashSet<string>();
            foreach (var state in docks)
            {
                var ent = _entManager.GetEntity(state.Entity);
                var position = state.Coordinates.Position;
                var uiPosition = matrix.Transform(position);

                if (uiPosition.Length() > WorldRange - dockScale)
                    continue;

                var color = HighlightedDock == state.Entity ? state.HighlightedColor : state.Color;

                uiPosition.Y = -uiPosition.Y;

                var verts = new[]
                {
                    matrix.Transform(position + new Vector2(-dockScale, -dockScale)),
                    matrix.Transform(position + new Vector2(dockScale, -dockScale)),
                    matrix.Transform(position + new Vector2(dockScale, dockScale)),
                    matrix.Transform(position + new Vector2(-dockScale, dockScale)),
                };

                for (var i = 0; i < verts.Length; i++)
                {
                    var vert = verts[i];
                    vert.Y = -vert.Y;
                    verts[i] = ScalePosition(vert);
                }

                handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, verts, color.WithAlpha(0.8f));
                handle.DrawPrimitives(DrawPrimitiveTopology.LineStrip, verts, color);

                // draw dock label
                if (state.Name != null && !labeled.Contains(state.Name))
                {
                    labeled.Add(state.Name);
                    Label label;
                    if (!_iffControls.TryGetValue(ent, out var control))
                    {
                        label = new Label();
                        _iffControls[ent] = label;
                        AddChild(label);
                    }
                    else
                    {
                        label = (Label) control;
                    }
                    shown.Add(ent);
                    label.Visible = true;
                    label.Text = state.Name;
                    LayoutContainer.SetPosition(label, ScalePosition(uiPosition) / UIScale);
                }
            }
        }
    }

    private void DrawGrid(DrawingHandleScreen handle, Matrix3 matrix, MapGridComponent grid, Color color, bool drawInterior)
    {
        var rator = grid.GetAllTilesEnumerator();
        var edges = new ValueList<Vector2>();

        while (rator.MoveNext(out var tileRef))
        {
            // TODO: Short-circuit interior chunk nodes
            // This can be optimised a lot more if required.
            Vector2? tileVec = null;

            // Iterate edges and see which we can draw
            for (var i = 0; i < 4; i++)
            {
                var dir = (DirectionFlag) Math.Pow(2, i);
                var dirVec = dir.AsDir().ToIntVec();

                if (!grid.GetTileRef(tileRef.Value.GridIndices + dirVec).Tile.IsEmpty)
                    continue;

                Vector2 start;
                Vector2 end;
                tileVec ??= (Vector2) tileRef.Value.GridIndices * grid.TileSize;

                // Draw line
                // Could probably rotate this but this might be faster?
                switch (dir)
                {
                    case DirectionFlag.South:
                        start = tileVec.Value;
                        end = tileVec.Value + new Vector2(grid.TileSize, 0f);
                        break;
                    case DirectionFlag.East:
                        start = tileVec.Value + new Vector2(grid.TileSize, 0f);
                        end = tileVec.Value + new Vector2(grid.TileSize, grid.TileSize);
                        break;
                    case DirectionFlag.North:
                        start = tileVec.Value + new Vector2(grid.TileSize, grid.TileSize);
                        end = tileVec.Value + new Vector2(0f, grid.TileSize);
                        break;
                    case DirectionFlag.West:
                        start = tileVec.Value + new Vector2(0f, grid.TileSize);
                        end = tileVec.Value;
                        break;
                    default:
                        throw new NotImplementedException();
                }

                var adjustedStart = matrix.Transform(start);
                var adjustedEnd = matrix.Transform(end);

                if (adjustedStart.Length() > ActualRadarRange || adjustedEnd.Length() > ActualRadarRange)
                    continue;

                start = ScalePosition(new Vector2(adjustedStart.X, -adjustedStart.Y));
                end = ScalePosition(new Vector2(adjustedEnd.X, -adjustedEnd.Y));

                edges.Add(start);
                edges.Add(end);
            }
        }

        handle.DrawPrimitives(DrawPrimitiveTopology.LineList, edges.Span, color);
    }

    private Vector2 ScalePosition(Vector2 value)
    {
        return value * MinimapScale + MidpointVector;
    }

    private Vector2 InverseScalePosition(Vector2 value)
    {
        return (value - MidpointVector) / MinimapScale;
    }
}
