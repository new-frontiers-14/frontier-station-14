using Content.Client._NF.Radar;
using Content.Client._NF.River;
using Content.Client.Station;
using Content.Client.UserInterface.Controls;
using Content.Shared._NF.Radar;
using Content.Shared._NF.Shuttles.Events;
using Content.Shared.Interaction;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Collections;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;
using System.Net;
using System.Numerics;

// Purposefully colliding with base namespace.
namespace Content.Client.Shuttles.UI;

public sealed partial class ShuttleNavControl
{
    // Dependency
    private readonly StationSystem _station;
    private readonly RadarBlipSystem _blips;
    private readonly RiverNodeSystem _rivers;

    // Constants for gunnery system
    // These 2 handle timing updates
    private const float RadarUpdateInterval = 0f;
    private const float FireRateLimit = 0.1f; // 100ms between shots
    private static readonly Color TargetColor = Color.FromHex("#99ff66");
    private float _updateAccumulator = 0f;

    private bool _isMouseDown;
    private bool _isMouseInside;
    private Vector2 _lastMousePos;
    private float _lastFireTime;

    // Constants for IFF system
    public float MaximumIFFDistance { get; set; } = -1f;
    public bool HideCoords { get; set; } = false;
    private static Color _dockLabelColor = Color.White;
    public bool HideTarget { get; set; } = false;
    public Vector2? Target { get; set; } = null;
    public NetEntity? TargetEntity { get; set; } = null;

    public InertiaDampeningMode DampeningMode { get; set; }
    public ServiceFlags ServiceFlags { get; set; } = ServiceFlags.None;

    // Constants for River System
    private const float FlowIndicatorLimit = 10f;
    private const float FlowSpeed = 15f;

    private float _flowIndicatorOffset = 0f;

    /// <summary>
    /// Updates the radar UI with the latest navigation state and sets additional NF-specific state.
    /// </summary>
    /// <param name="state">The navigation interface state.</param>
    private void NFUpdateState(NavInterfaceState state)
    {
        if (state.MaxIffRange != null)
            MaximumIFFDistance = state.MaxIffRange.Value;
        HideCoords = state.HideCoords;
        Target = state.Target;
        TargetEntity = state.TargetEntity;
        HideTarget = state.HideTarget;

        if (!EntManager.GetCoordinates(state.Coordinates).HasValue ||
            !EntManager.TryGetComponent(EntManager.GetCoordinates(state.Coordinates).GetValueOrDefault().EntityId, out TransformComponent? transform) ||
            !EntManager.HasComponent<PhysicsComponent>(transform.GridUid))
        {
            return;
        }

        DampeningMode = state.DampeningMode;
        ServiceFlags = state.ServiceFlags;
    }

    /// <summary>
    /// Checks if an IFF marker should be drawn based on distance and maximum IFF range.
    /// </summary>
    /// <param name="shouldDrawIff">Whether the IFF marker would otherwise be drawn.</param>
    /// <param name="distance">The distance vector to the object.</param>
    /// <returns>True if the IFF marker should be drawn, false otherwise.</returns>
    private bool NFCheckShouldDrawIffRangeCondition(bool shouldDrawIff, Vector2 distance)
    {
        if (shouldDrawIff && MaximumIFFDistance >= 0.0f)
        {
            if (distance.Length() > MaximumIFFDistance)
            {
                shouldDrawIff = false;
            }
        }
        return shouldDrawIff;
    }

    /// <summary>
    /// Adds a blip to the blip data list for later drawing.
    /// </summary>
    private static void NFAddBlipToList(List<BlipData> blipDataList, bool isOutsideRadarCircle, Vector2 uiPosition, int uiXCentre, int uiYCentre, Color color)
    {
        blipDataList.Add(new BlipData
        {
            IsOutsideRadarCircle = isOutsideRadarCircle,
            UiPosition = uiPosition,
            VectorToPosition = uiPosition - new Vector2(uiXCentre, uiYCentre),
            Color = color
        });
    }

    /// <summary>
    /// Adds blip style triangles that are on ships or pointing towards ships on the edges of the radar.
    /// Draws blips at the BlipData's uiPosition and uses VectorToPosition to rotate to point towards ships.
    /// </summary>
    private void NFDrawBlips(DrawingHandleBase handle, List<BlipData> blipDataList)
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
                var angle = (float)Math.Atan2(blipData.VectorToPosition.Y, blipData.VectorToPosition.X) + -1.6f;

                // Manually create a rotation matrix
                var cos = (float)Math.Cos(angle);
                var sin = (float)Math.Sin(angle);
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
    //TODO: Add summaries
    private void NFDrawRivers(DrawingHandleBase handle, Matrix3x2 worldToView, Box2 viewAABB)
    {

        var nodes = _rivers.GetNodeList();
        if (nodes != null)
        {
            foreach (var node in nodes)
            {
                if (!_rivers.TryGetNodeData(node, out var nodeComp))
                {
                    continue;
                }
                var anySegmentInView = false;
                var nextNodes = _rivers.GetNextNodes(nodeComp);
                foreach (var nextNode in nextNodes)
                {
                    if (!_rivers.TryGetNodeData(nextNode, out var nextNodeComp))
                    {
                        continue;
                    }
                    //Check for culling, makes a bounding box around the segment, and checks if it intersects the viewport.
                    //TODO: Figure out a tighter fit, less naive.
                    var nodeXForm = nodeComp.Location;
                    var nextNodeXForm = _transform.GetWorldPosition(nextNode);
                    var control1XForm = nodeXForm + nodeComp.ForwardControlPoint;
                    var control2XForm = nodeXForm + -nextNodeComp.ForwardControlPoint;
                    var centrePoint = (nodeXForm + control1XForm + control2XForm + nextNodeXForm) / 4f;

                    var maxX = MathF.Max(MathF.Max(nodeXForm.X, control1XForm.X), MathF.Max(control2XForm.X, nextNodeXForm.X)) + nodeComp.NodeRange;
                    var maxY = MathF.Max(MathF.Max(nodeXForm.Y, control1XForm.Y), MathF.Max(control2XForm.Y, nextNodeXForm.Y)) + nodeComp.NodeRange;
                    var minX = MathF.Min(MathF.Min(nodeXForm.X, control1XForm.X), MathF.Min(control2XForm.X, nextNodeXForm.X)) - nodeComp.NodeRange;
                    var minY = MathF.Min(MathF.Min(nodeXForm.Y, control1XForm.Y), MathF.Min(control2XForm.Y, nextNodeXForm.Y)) - nodeComp.NodeRange;

                    var boxHeight = maxY - minY;
                    var boxWidth = maxX - minX;
                    var boxSize = new Vector2(boxWidth, boxHeight);

                    var nodeToWorld = _transform.GetWorldMatrix(node);

                    var boundingBox = Box2.CenteredAround(centrePoint, boxSize);

                    if (!boundingBox.Intersects(viewAABB))
                    {
                        continue;
                    }
                    anySegmentInView = true;
                    //End of culling

                    var pathColour = Color.Blue;

                    var totalSections = 5;
                    //var bezier = _rivers.CalcCurveSections(nodeComp.Location, nextNode.ControlPoint, nextNodeComp.Location, totalSections);
                    Vector2 controlPoint1 = nodeComp.Location + nodeComp.ForwardControlPoint;
                    Vector2 controlPoint2 = nextNodeComp.Location + -nextNodeComp.ForwardControlPoint;
                    var bezier = RiverNodeSystem.CalcCubicCurveSections(nodeComp.Location, controlPoint1, controlPoint2, nextNodeComp.Location, totalSections);
                    for (var i = 0; i < bezier.Count - 1; i++)
                    {
                        var segmentStart = Vector2.Transform(bezier[i], worldToView);
                        var segmentEnd = Vector2.Transform(bezier[i + 1], worldToView);

                        var coolHandle = (DrawingHandleScreen)handle; //DrawingHandleBase doesn't support dotted lines.
                        var dashLength = 5 * MinimapScale;
                        var gapLength = 5 * MinimapScale;

                        coolHandle.DrawDottedLine(segmentStart, segmentEnd, pathColour,
                            dashSize: dashLength,
                            gapSize: gapLength,
                            offset: _flowIndicatorOffset * FlowSpeed * MinimapScale);
                        //var faintMagenta = Color.Magenta;
                        //faintMagenta.A = 0.01f;
                        //handle.DrawCircle(segmentStart, nodeComp.NodeRange * MinimapScale, faintMagenta);
                    }

                    handle.DrawCircle(Vector2.Transform(controlPoint1, worldToView), 5f * MinimapScale, Color.Magenta);
                    handle.DrawCircle(Vector2.Transform(controlPoint2, worldToView), 5f * MinimapScale, Color.Lime);
                }
                if (!anySegmentInView)
                {
                    continue;
                }
                // Testing
                for (var i = 0; i < _rivers.MiddleLine.Count; i += 2)
                {
                    var lineStart = _rivers.MiddleLine[i];
                    var lineEnd = _rivers.MiddleLine[i + 1];
                    if (lineStart != null && lineEnd != null)
                    {
                        var xformedLineStart = Vector2.Transform((Vector2)lineStart, worldToView);
                        var xformedLineEnd = Vector2.Transform((Vector2)lineEnd, worldToView);
                        handle.DrawLine(xformedLineStart, xformedLineEnd, Color.Magenta);
                    }
                }
                // End Testing
                var meshData = _rivers.MeshData[node];
                Vector2[] renderMesh = new Vector2[meshData.Length];
                for (var i = 0; i < meshData.Length; i++)
                {
                    renderMesh[i] = Vector2.Transform(meshData[i], worldToView);
                }
                var colour = Color.DarkGray;
                colour.A = 0.05f;
                handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, new Span<Vector2>(renderMesh, 0, renderMesh.Length), colour);
            }
        }
    }

    private void HandleMouseEntered(GUIMouseHoverEventArgs args)
    {
        _isMouseInside = true;
    }

    private void HandleMouseExited(GUIMouseHoverEventArgs args)
    {
        _isMouseInside = false;
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        _isMouseDown = true;
        _lastMousePos = args.RelativePosition;
        TryFireAtPosition(args.RelativePosition);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        _updateAccumulator += args.DeltaSeconds;

        if (_updateAccumulator >= RadarUpdateInterval)
        {
            _updateAccumulator = 0; // I'm not subtracting because frame updates can majorly lag in a way normal ones cannot.

            if (_consoleEntity != null)
                _blips.RequestBlips((EntityUid)_consoleEntity);
        }

        _flowIndicatorOffset += args.DeltaSeconds;
        if (_flowIndicatorOffset >= FlowIndicatorLimit * MinimapScale)
        {
            _flowIndicatorOffset = 0;
        }

        if (_isMouseDown && _isMouseInside)
        {
            var currentTime = IoCManager.Resolve<IGameTiming>().CurTime.TotalSeconds;
            if (currentTime - _lastFireTime >= FireRateLimit)
            {
                var mousePos = UserInterfaceManager.MousePositionScaled;
                var relativePos = mousePos.Position - GlobalPosition;
                if (relativePos != _lastMousePos)
                {
                    _lastMousePos = relativePos;
                }
                TryFireAtPosition(relativePos);
                _lastFireTime = (float)currentTime;
            }
        }
    }
    private void TryFireAtPosition(Vector2 relativePosition)
    {
        if (_coordinates == null || _rotation == null || OnRadarClick == null)
            return;

        var a = InverseScalePosition(relativePosition);
        var relativeWorldPos = new Vector2(a.X, -a.Y);
        relativeWorldPos = _rotation.Value.RotateVec(relativeWorldPos);
        var coords = _coordinates.Value.Offset(relativeWorldPos);
        OnRadarClick?.Invoke(coords);
    }

    private void DrawBlipShape(DrawingHandleScreen handle, Vector2 position, float size, Color color, RadarBlipShape shape)
    {
        switch (shape)
        {
            case RadarBlipShape.Circle:
                handle.DrawCircle(position, size, color);
                break;
            case RadarBlipShape.Square:
                var halfSize = size / 2;
                var rect = new UIBox2(
                    position.X - halfSize,
                    position.Y - halfSize,
                    position.X + halfSize,
                    position.Y + halfSize
                );
                handle.DrawRect(rect, color);
                break;
            case RadarBlipShape.Triangle:
                var points = new Vector2[]
                {
                position + new Vector2(0, -size),
                position + new Vector2(-size * 0.866f, size * 0.5f),
                position + new Vector2(size * 0.866f, size * 0.5f)
                };
                handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, points, color);
                break;
            case RadarBlipShape.Star:
                DrawStar(handle, position, size, color);
                break;
            case RadarBlipShape.Diamond:
                var diamondPoints = new Vector2[]
                {
                position + new Vector2(0, -size),
                position + new Vector2(size, 0),
                position + new Vector2(0, size),
                position + new Vector2(-size, 0)
                };
                handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, diamondPoints, color);
                break;
            case RadarBlipShape.Hexagon:
                DrawHexagon(handle, position, size, color);
                break;
            case RadarBlipShape.Arrow:
                DrawArrow(handle, position, size, color);
                break;
        }
    }

    private void DrawStar(DrawingHandleScreen handle, Vector2 position, float size, Color color)
    {
        const int points = 5;
        const float innerRatio = 0.4f;
        var vertices = new Vector2[points * 2 + 2]; // outer and inner point, five times, plus a center point and the original drawn point

        vertices[0] = position;
        for (var i = 0; i <= points * 2; i++)
        {
            var angle = i * Math.PI / points;
            var radius = i % 2 == 0 ? size : size * innerRatio;
            vertices[i + 1] = position + new Vector2(
                (float)Math.Sin(angle) * radius,
                -(float)Math.Cos(angle) * radius
            );
        }

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, vertices, color);
    }

    private void DrawHexagon(DrawingHandleScreen handle, Vector2 position, float size, Color color)
    {
        var vertices = new Vector2[6];
        for (var i = 0; i < 6; i++)
        {
            var angle = i * Math.PI / 3;
            vertices[i] = position + new Vector2(
                (float)Math.Sin(angle) * size,
                -(float)Math.Cos(angle) * size
            );
        }

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, vertices, color);
    }

    private void DrawArrow(DrawingHandleScreen handle, Vector2 position, float size, Color color)
    {
        var vertices = new Vector2[]
        {
        position + new Vector2(0, -size),           // Tip
        position + new Vector2(-size * 0.5f, 0),    // Left wing
        position + new Vector2(0, size * 0.5f),     // Bottom
        position + new Vector2(size * 0.5f, 0)      // Right wing
        };

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, vertices, color);
    }
}
