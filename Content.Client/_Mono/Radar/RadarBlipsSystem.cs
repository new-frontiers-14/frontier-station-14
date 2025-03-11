using System.Numerics;
using Content.Shared._Mono.Radar;
using Robust.Shared.Timing;

namespace Content.Client._Mono.Radar;

public sealed partial class RadarBlipsSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    
    private TimeSpan _lastUpdatedTime;
    private List<(NetEntity? Grid, Vector2 Position, float Scale, Color Color, RadarBlipShape Shape)> _blips = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<GiveBlipsEvent>(HandleReceiveBlips);
    }

    private void HandleReceiveBlips(GiveBlipsEvent ev, EntitySessionEventArgs args)
    {
        _blips = ev.Blips;
        _lastUpdatedTime = _timing.CurTime;
    }

    public void RequestBlips(EntityUid console)
    {
        // Only request if we have a valid console
        if (!Exists(console))
            return;
            
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
        // If it's been more than a second since our last update,
        // the data is considered stale - return an empty list
        if (_timing.CurTime.TotalSeconds - _lastUpdatedTime.TotalSeconds > 1)
            return new List<(Vector2, float, Color, RadarBlipShape)>();

        var result = new List<(Vector2, float, Color, RadarBlipShape)>(_blips.Count);
        
        foreach (var blip in _blips)
        {
            // If no grid, position is already in world coordinates
            if (blip.Grid == null)
            {
                result.Add((blip.Position, blip.Scale, blip.Color, blip.Shape));
                continue;
            }
            
            // If grid exists, transform from grid-local to world coordinates
            if (TryGetEntity(blip.Grid, out var gridEntity))
            {
                // Transform the grid-local position to world position
                var worldPos = _xform.GetWorldPosition(gridEntity.Value);
                var gridRot = _xform.GetWorldRotation(gridEntity.Value);
                
                // Rotate the local position by grid rotation and add grid position
                var rotatedLocalPos = gridRot.RotateVec(blip.Position);
                var finalWorldPos = worldPos + rotatedLocalPos;
                
                result.Add((finalWorldPos, blip.Scale, blip.Color, blip.Shape));
            }
            else
            {
                // Grid not found, skip this blip
                continue;
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Gets the raw blips data which includes grid information for more accurate rendering.
    /// </summary>
    public List<(NetEntity? Grid, Vector2 Position, float Scale, Color Color, RadarBlipShape Shape)> GetRawBlips()
    {
        if (_timing.CurTime.TotalSeconds - _lastUpdatedTime.TotalSeconds > 1)
            return new List<(NetEntity? Grid, Vector2 Position, float Scale, Color Color, RadarBlipShape Shape)>();
            
        return _blips;
    }
} 