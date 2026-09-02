using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server._NF.DangerTether;

/// <summary>
/// A system to handle tethering dangerous objects, and deleting them when out of range of any tether.
/// Runs periodic checks to handle deletion.
/// </summary>
public sealed class DangerTetherSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private TransformSystem _transform = default!;

    private readonly TimeSpan _scanPeriod = TimeSpan.FromSeconds(0.5);
    private TimeSpan _nextScan = TimeSpan.Zero;
    private List<(MapCoordinates Position, float Distance)> _tethers = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DangerTetheredComponent, MapInitEvent>(OnTetheredMapInit);
    }

    /// <summary>
    /// Update: periodically, check that all DangerTethered entities are in range of a tether.
    /// If they aren't, delete them.
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextScan)
            return;

        _nextScan += _scanPeriod;

        PopulateTetherList();

        var tetheredQuery = EntityQueryEnumerator<DangerTetheredComponent>();
        while (tetheredQuery.MoveNext(out var targetUid, out _))
        {
            if (!AnyTetherInRange(targetUid))
                QueueDel(targetUid);
        }
    }

    /// <summary>
    /// DangerTethered MapInit: must be in range of a tether, otherwise delete it.
    /// </summary>
    private void OnTetheredMapInit(Entity<DangerTetheredComponent> ent, ref MapInitEvent args)
    {
        PopulateTetherList();
        if (!AnyTetherInRange(ent))
            QueueDel(ent);
    }

    private void PopulateTetherList()
    {
        _tethers.Clear();
        var tetherQuery = EntityQueryEnumerator<DangerTetherComponent>();
        while (tetherQuery.MoveNext(out var tetherUid, out var tether))
        {
            _tethers.Add((_transform.GetMapCoordinates(tetherUid), tether.MaxDistance));
        }
    }

    public bool AnyTetherInRange(EntityUid ent)
    {
        var targetCoords = _transform.GetMapCoordinates(ent);
        foreach (var tetherEntry in _tethers)
        {
            if (tetherEntry.Position.MapId != targetCoords.MapId)
                continue;

            if (tetherEntry.Position.InRange(targetCoords, tetherEntry.Distance))
                return true;
        }

        return false;
    }
}
