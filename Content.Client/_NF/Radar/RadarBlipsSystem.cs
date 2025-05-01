using System.Numerics;
using Content.Shared._NF.Radar;
using Robust.Shared.Timing;

namespace Content.Client._NF.Radar;

public sealed partial class RadarBlipsSystem : EntitySystem
{
    private const double BlipStaleSeconds = 1.0;
    private static readonly List<(Vector2, float, Color, RadarBlipShape)> EmptyBlipList = new();
    private static readonly List<(NetEntity? Grid, Vector2 Position, float Scale, Color Color, RadarBlipShape Shape)> EmptyRawBlipList = new();
    private TimeSpan _lastRequestTime = TimeSpan.Zero;
    private static readonly TimeSpan RequestThrottle = TimeSpan.FromMilliseconds(250);

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    private TimeSpan _lastUpdatedTime;
    private List<(NetEntity? Grid, Vector2 Position, float Scale, Color Color, RadarBlipShape Shape)> _blips = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<GiveBlipsEvent>(HandleReceiveBlips);
    }

    /// <summary>
    /// Handles receiving blip data from the server.
    /// </summary>
    private void HandleReceiveBlips(GiveBlipsEvent ev, EntitySessionEventArgs args)
    {
        if (ev?.Blips == null)
        {
            _blips = EmptyRawBlipList;
            return;
        }
        _blips = ev.Blips;
        _lastUpdatedTime = _timing.CurTime;
    }

    /// <summary>
    /// Requests blip data from the server for the given radar console, throttled to avoid spamming.
    /// </summary>
    public void RequestBlips(EntityUid console)
    {
        if (!Exists(console))
            return;
        if (_timing.CurTime - _lastRequestTime < RequestThrottle)
            return;
        _lastRequestTime = _timing.CurTime;
        var netConsole = GetNetEntity(console);
        var ev = new RequestBlipsEvent(netConsole);
        RaiseNetworkEvent(ev);
    }

    /// <summary>
    /// Gets the current blips as world positions with their scale, color and shape.
    /// This is needed for the legacy radar display that expects world coordinates.
    /// </summary>
    public List<(Vector2, float, Color, RadarBlipShape)> GetCurrentBlips()
    {
        if (_timing.CurTime.TotalSeconds - _lastUpdatedTime.TotalSeconds > BlipStaleSeconds)
            return EmptyBlipList;
        var result = new List<(Vector2, float, Color, RadarBlipShape)>(_blips.Count);
        foreach (var blip in _blips)
        {
            if (blip.Grid == null)
            {
                result.Add((blip.Position, blip.Scale, blip.Color, blip.Shape));
                continue;
            }
            if (TryGetEntity(blip.Grid, out var gridEntity))
            {
                var worldPos = _xform.GetWorldPosition(gridEntity.Value);
                var gridRot = _xform.GetWorldRotation(gridEntity.Value);
                var rotatedLocalPos = gridRot.RotateVec(blip.Position);
                var finalWorldPos = worldPos + rotatedLocalPos;
                result.Add((finalWorldPos, blip.Scale, blip.Color, blip.Shape));
            }
        }
        return result;
    }

    /// <summary>
    /// Gets the raw blips data which includes grid information for more accurate rendering.
    /// </summary>
    public List<(NetEntity? Grid, Vector2 Position, float Scale, Color Color, RadarBlipShape Shape)> GetRawBlips()
    {
        if (_timing.CurTime.TotalSeconds - _lastUpdatedTime.TotalSeconds > BlipStaleSeconds)
            return EmptyRawBlipList;
        return _blips;
    }
}
