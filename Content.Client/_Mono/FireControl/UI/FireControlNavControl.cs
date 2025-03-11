using System.Linq;
using System.Numerics;
using Content.Client.Shuttles.UI;
using Content.Shared._Mono.FireControl;
using Content.Shared.Physics;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Content.Client._Mono.Radar;
using Content.Shared._Mono.Radar;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Client._Mono.FireControl.UI;

public sealed class FireControlNavControl : BaseShuttleControl
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    private readonly SharedShuttleSystem _shuttles;
    private readonly SharedTransformSystem _transform;
    private readonly IEntitySystemManager _sysManager = default!;
    private readonly RadarBlipsSystem _blips;
    private readonly SharedPhysicsSystem _physics;

    private EntityCoordinates? _coordinates;
    private EntityUid? _consoleEntity;
    private Angle? _rotation;
    private Dictionary<NetEntity, List<DockingPortState>> _docks = new();

    private EntityUid? _activeConsole;
    private FireControllableEntry[]? _controllables;

    private List<Entity<MapGridComponent>> _grids = new();

    #region Mono

    private const float RadarUpdateInterval = 0f;
    private float _updateAccumulator = 0f;
    #endregion

    private bool _isMouseDown;
    private bool _isMouseInside;
    private Vector2 _lastMousePos;
    private float _lastFireTime;
    private const float FireRateLimit = 0.1f;

    public Action<EntityCoordinates>? OnRadarClick;
    public bool ShowIFF { get; set; } = true;
    public bool RotateWithEntity { get; set; } = true;

    public FireControlNavControl() : base(64f, 512f, 512f)
    {
        IoCManager.InjectDependencies(this);
        _shuttles = EntManager.System<SharedShuttleSystem>();
        _transform = EntManager.System<SharedTransformSystem>();
        _blips = EntManager.System<RadarBlipsSystem>();
        _physics = EntManager.System<SharedPhysicsSystem>();

        OnMouseEntered += HandleMouseEntered;
        OnMouseExited += HandleMouseExited;
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);
        if (_isMouseInside)
        {
            _lastMousePos = args.RelativePosition;
        }
    }

    private void HandleMouseEntered(GUIMouseHoverEventArgs args)
    {
        _isMouseInside = true;
        _lastMousePos = UserInterfaceManager.MousePositionScaled.Position - GlobalPosition;
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
        TryFireAtPosition(_lastMousePos);
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);

        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        _isMouseDown = false;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        _updateAccumulator += args.DeltaSeconds;

        if (_updateAccumulator >= RadarUpdateInterval)
        {
            _updateAccumulator = 0;

            if (_consoleEntity != null)
                _blips.RequestBlips((EntityUid)_consoleEntity);
        }

        if (_isMouseDown && _isMouseInside)
        {
            var currentTime = IoCManager.Resolve<IGameTiming>().CurTime.TotalSeconds;
            if (currentTime - _lastFireTime >= FireRateLimit)
            {
                var mousePos = UserInterfaceManager.MousePositionScaled.Position - GlobalPosition;
                if (mousePos != _lastMousePos)
                {
                    _lastMousePos = mousePos;
                }
                TryFireAtPosition(_lastMousePos);
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

    public void SetMatrix(EntityCoordinates? coordinates, Angle? angle)
    {
        _coordinates = coordinates;
        _rotation = angle;
    }

    public void SetConsole(EntityUid? consoleEntity)
    {
        _consoleEntity = consoleEntity;
    }

    public void UpdateState(NavInterfaceState state)
    {
        SetMatrix(EntManager.GetCoordinates(state.Coordinates), state.Angle);
        _docks = state.Docks;
        RotateWithEntity = state.RotateWithEntity;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        DrawBacking(handle);
        DrawCircles(handle);

        if (_coordinates == null || _rotation == null)
        {
            DrawNoSignal(handle);
            return;
        }

        var xformQuery = EntManager.GetEntityQuery<TransformComponent>();
        var fixturesQuery = EntManager.GetEntityQuery<FixturesComponent>();
        var bodyQuery = EntManager.GetEntityQuery<PhysicsComponent>();

        if (!xformQuery.TryGetComponent(_coordinates.Value.EntityId, out var xform)
            || xform.MapID == MapId.Nullspace)
        {
            return;
        }

        var mapPos = _transform.ToMapCoordinates(_coordinates.Value);
        var posMatrix = Matrix3Helpers.CreateTransform(_coordinates.Value.Position, _rotation.Value);
        var ourEntRot = RotateWithEntity ? _transform.GetWorldRotation(xform) : _rotation.Value;
        var ourEntMatrix = Matrix3Helpers.CreateTransform(_transform.GetWorldPosition(xform), ourEntRot);
        var shuttleToWorld = Matrix3x2.Multiply(posMatrix, ourEntMatrix);
        Matrix3x2.Invert(shuttleToWorld, out var worldToShuttle);
        var shuttleToView = Matrix3x2.CreateScale(new Vector2(MinimapScale, -MinimapScale)) * Matrix3x2.CreateTranslation(MidPointVector);

        var ourGridId = xform.GridUid;
        if (EntManager.TryGetComponent<MapGridComponent>(ourGridId, out var ourGrid) &&
            fixturesQuery.HasComponent(ourGridId.Value))
        {
            var ourGridToWorld = _transform.GetWorldMatrix(ourGridId.Value);
            var ourGridToShuttle = Matrix3x2.Multiply(ourGridToWorld, worldToShuttle);
            var ourGridToView = ourGridToShuttle * shuttleToView;
            var color = _shuttles.GetIFFColor(ourGridId.Value, self: true);

            DrawGrid(handle, ourGridToView, (ourGridId.Value, ourGrid), color);
        }

        const float radarVertRadius = 2f;
        var radarPosVerts = new Vector2[]
        {
            ScalePosition(new Vector2(0f, -radarVertRadius)),
            ScalePosition(new Vector2(radarVertRadius / 2f, 0f)),
            ScalePosition(new Vector2(0f, radarVertRadius)),
            ScalePosition(new Vector2(radarVertRadius / -2f, 0f)),
        };

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, radarPosVerts, Color.Lime);

        _grids.Clear();
        var maxRange = new Vector2(WorldRange, WorldRange);
        _mapManager.FindGridsIntersecting(xform.MapID, new Box2(mapPos.Position - maxRange, mapPos.Position + maxRange), ref _grids, approx: true, includeMap: false);

        foreach (var grid in _grids)
        {
            var gUid = grid.Owner;
            if (gUid == ourGridId || !fixturesQuery.HasComponent(gUid))
                continue;

            var gridBody = bodyQuery.GetComponent(gUid);
            EntManager.TryGetComponent<IFFComponent>(gUid, out var iff);

            if (!_shuttles.CanDraw(gUid, gridBody, iff))
                continue;

            var curGridToWorld = _transform.GetWorldMatrix(gUid);
            var curGridToView = curGridToWorld * worldToShuttle * shuttleToView;

            var labelColor = _shuttles.GetIFFColor(grid, self: false, iff);
            var coordColor = new Color(labelColor.R * 0.8f, labelColor.G * 0.8f, labelColor.B * 0.8f, 0.5f);

            DrawGrid(handle, curGridToView, grid, labelColor);

            if (ShowIFF)
            {
                var labelName = _shuttles.GetIFFLabel(grid, self: false, iff);
                if (labelName != null)
                {
                    var gridBounds = grid.Comp.LocalAABB;
                    var gridCentre = Vector2.Transform(gridBody.LocalCenter, curGridToView);

                    var distance = gridCentre.Length();
                    var labelText = Loc.GetString("shuttle-console-iff-label", ("name", labelName),
                        ("distance", $"{distance:0.0}"));

                    var mapCoords = _transform.GetWorldPosition(gUid);
                    var coordsText = $"({mapCoords.X:0.0}, {mapCoords.Y:0.0})";

                    var labelDimensions = handle.GetDimensions(Font, labelText, 1f);
                    var coordsDimensions = handle.GetDimensions(Font, coordsText, 0.7f);

                    var yOffset = Math.Max(gridBounds.Height, gridBounds.Width) * MinimapScale / 1.8f;

                    var gridScaledPosition = gridCentre - new Vector2(0, -yOffset);

                    var gridOffset = gridScaledPosition / PixelSize - new Vector2(0.5f, 0.5f);
                    var offsetMax = Math.Max(Math.Abs(gridOffset.X), Math.Abs(gridOffset.Y)) * 2f;
                    if (offsetMax > 1)
                    {
                        gridOffset = new Vector2(gridOffset.X / offsetMax, gridOffset.Y / offsetMax);
                        gridScaledPosition = (gridOffset + new Vector2(0.5f, 0.5f)) * PixelSize;
                    }

                    var labelUiPosition = gridScaledPosition - new Vector2(labelDimensions.X / 2f, 0);
                    var coordUiPosition = gridScaledPosition - new Vector2(coordsDimensions.X / 2f, -labelDimensions.Y);

                    var controlExtents = PixelSize - new Vector2(labelDimensions.X, labelDimensions.Y);
                    labelUiPosition = Vector2.Clamp(labelUiPosition, Vector2.Zero, controlExtents);

                    handle.DrawString(Font, labelUiPosition, labelText, labelColor);

                    if (offsetMax < 1)
                    {
                        handle.DrawString(Font, coordUiPosition, coordsText, 0.7f, coordColor);
                    }
                }
            }
        }

        #region Mono

        var updateRatio = _updateAccumulator / RadarUpdateInterval;

        Angle angle = updateRatio * Math.Tau;
        var origin = ScalePosition(-new Vector2(Offset.X, -Offset.Y));
        handle.DrawLine(origin, origin + angle.ToVec() * ScaledMinimapRadius * 1.42f, Color.Red.WithAlpha(0.1f));

        var blips = _blips.GetCurrentBlips();

        foreach (var blip in blips)
        {
            var blipPos = Vector2.Transform(blip.Item1, worldToShuttle * shuttleToView);
            DrawBlipShape(handle, blipPos, blip.Item2 * 3f, blip.Item3.WithAlpha(0.8f), blip.Item4);

            if (_isMouseInside && _controllables != null)
            {
                var worldPos = blip.Item1;
                var isFireControllable = _controllables.Any(c => {
                    var coords = EntManager.GetCoordinates(c.Coordinates);
                    var entityMapPos = _transform.ToMapCoordinates(coords);
                    return Vector2.Distance(entityMapPos.Position, worldPos) < 0.1f;
                });

                if (isFireControllable)
                {
                    var cursorViewPos = InverseScalePosition(_lastMousePos);
                    cursorViewPos = ScalePosition(cursorViewPos);

                    Matrix3x2.Invert(worldToShuttle * shuttleToView, out var viewToWorld);
                    var cursorWorldPos = Vector2.Transform(cursorViewPos, viewToWorld);

                    var direction = cursorWorldPos - worldPos;
                    var ray = new CollisionRay(worldPos, direction.Normalized(), (int)CollisionGroup.Impassable);

                    var results = _physics.IntersectRay(xform.MapID, ray, direction.Length(), ignoredEnt: _coordinates?.EntityId);

                    if (!results.Any())
                    {
                        handle.DrawLine(blipPos, cursorViewPos, blip.Item3.WithAlpha(0.3f));
                    }
                }
            }
        }
        #endregion
    }

    public void UpdateControllables(EntityUid console, FireControllableEntry[] controllables)
    {
        _activeConsole = console;
        _controllables = controllables;
    }

    private Vector2 InverseScalePosition(Vector2 value)
    {
        return (value - MidPointVector) / MinimapScale;
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
        var outerRadius = size;
        var innerRadius = size * 0.4f;
        var points = new List<Vector2>();

        for (var i = 0; i < 10; i++)
        {
            var angle = i * MathF.PI / 5;
            var radius = i % 2 == 0 ? outerRadius : innerRadius;
            points.Add(position + new Vector2(
                radius * MathF.Sin(angle),
                -radius * MathF.Cos(angle)
            ));
        }

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, points.ToArray(), color);
    }

    private void DrawHexagon(DrawingHandleScreen handle, Vector2 position, float size, Color color)
    {
        var points = new List<Vector2>();

        for (var i = 0; i < 6; i++)
        {
            var angle = i * MathF.PI / 3;
            points.Add(position + new Vector2(
                size * MathF.Cos(angle),
                size * MathF.Sin(angle)
            ));
        }

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, points.ToArray(), color);
    }

    private void DrawArrow(DrawingHandleScreen handle, Vector2 position, float size, Color color)
    {
        var points = new Vector2[]
        {
            position + new Vector2(0, -size),
            position + new Vector2(size * 0.5f, 0),
            position + new Vector2(0, size),
            position + new Vector2(-size * 0.5f, 0)
        };

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, points, color);
    }
}
