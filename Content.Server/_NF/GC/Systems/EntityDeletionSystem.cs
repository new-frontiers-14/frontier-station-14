using Content.Server._NF.GC.Components;
using Content.Server.GameTicking;
using Content.Server.Worldgen;
using Content.Server.Worldgen.Components;
using Content.Server.Worldgen.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Mind.Components;
using Content.Shared.Shuttles.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server._NF.GC.Systems;

/// <summary>
/// A garbage collection system.
/// Deletes unused entities parented on the main map if they've been on an unloaded chunk for a given number of passes.
/// Each pass runs at a configurable period.
/// </summary>
public sealed class EntityDeletionSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly WorldControllerSystem _world = default!;

    // Entity queries
    // EntityQuery<MapGridComponent> _mapGridQuery = default!;
    EntityQuery<MindContainerComponent> _mindContainerQuery = default!;
    EntityQuery<WorldControllerComponent> _worldControllerQuery = default!;
    EntityQuery<LoadedChunkComponent> _loadedChunkQuery = default!;

    // These two will be cloned from the map's transform component at regular intervals.
    // Their children will be maintained between runs.
    private EntityUid _defaultMapUid;
    private EntityUid _ftlMapUid;
    private HashSet<EntityUid> _defaultTransformSet = new();
    private HashSet<EntityUid> _ftlTransformSet = new();
    private bool _defaultEnumeratorValid = false;
    private HashSet<EntityUid>.Enumerator _defaultEnumerator = default!;
    private bool _ftlEnumeratorValid = false;
    private HashSet<EntityUid>.Enumerator _ftlEnumerator = default!;
    private TimeSpan _nextDefaultTime = TimeSpan.Zero;
    private TimeSpan _nextFtlTime = TimeSpan.Zero;

    // GC parameters - TODO: move these to CCVars
    private TimeSpan _censusPassPeriod = TimeSpan.FromMinutes(1);
    private int _censusEntitiesPerFrame = 64;
    private int _tallyMax = 3; // The number of tallies needed before queueing an entity to be deleted.
    public override void Initialize()
    {
        base.Initialize();

        // _mapGridQuery = GetEntityQuery<MapGridComponent>();
        _mindContainerQuery = GetEntityQuery<MindContainerComponent>();
        _worldControllerQuery = GetEntityQuery<WorldControllerComponent>();
        _loadedChunkQuery = GetEntityQuery<LoadedChunkComponent>();

        // TODO: reset tally on reparent
        SubscribeLocalEvent<DeletionPassTallyComponent, EntParentChangedMessage>(OnDeletionParentChanged);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent _)
    {
        _nextDefaultTime = TimeSpan.Zero;
        _nextFtlTime = TimeSpan.Zero;
        _defaultEnumerator = default!;
        _ftlEnumerator = default!;
        _defaultEnumeratorValid = false;
        _ftlEnumeratorValid = false;
        _defaultMapUid = EntityUid.Invalid;
        _ftlMapUid = EntityUid.Invalid;
    }

    private void OnDeletionParentChanged(Entity<DeletionPassTallyComponent> ent, ref EntParentChangedMessage args)
    {
        RemComp<DeletionPassTallyComponent>(ent);
    }

    public override void Update(float frameTime)
    {
        int count;
        if (!_defaultEnumeratorValid)
        {
            if (_timing.CurTime >= _nextDefaultTime)
            {
                if (_defaultMapUid == EntityUid.Invalid && _map.TryGetMap(_gameTicker.DefaultMap, out var mapUid))
                    _defaultMapUid = mapUid.Value;

                if (TryComp(_defaultMapUid, out TransformComponent? xform))
                {
                    _defaultTransformSet.Clear();
                    _defaultTransformSet.EnsureCapacity(xform.ChildCount);
                    var enumerator = xform.ChildEnumerator;
                    while (enumerator.MoveNext(out var uid))
                    {
                        _defaultTransformSet.Add(uid);
                    }
                    _defaultEnumerator = _defaultTransformSet.GetEnumerator();
                    _defaultEnumeratorValid = true;
                }
                _nextDefaultTime = _timing.CurTime + _censusPassPeriod;
            }
        }
        else
        {
            count = 0;
            _worldControllerQuery.TryComp(_defaultMapUid, out var worldController);
            while (_defaultEnumerator.MoveNext())
            {
                count++;
                var uid = _defaultEnumerator.Current;

                if (EntityManager.EntityExists(uid)
                    && TryComp(uid, out TransformComponent? xform)
                    && xform.ParentUid == _defaultMapUid
                    && (!_mindContainerQuery.TryComp(uid, out var mindContainer)
                    || !mindContainer.HasMind))
                {
                    if (!_world.TryGetChunk(WorldGen.WorldToChunkCoords(_transform.GetWorldPosition(xform)).Floored(), _defaultMapUid, out var chunk, worldController)
                        || !_loadedChunkQuery.TryGetComponent(chunk, out var loaded)
                        || loaded.Loaders == null
                        || loaded.Loaders.Count == 0)
                    {
                        var tally = EnsureComp<DeletionPassTallyComponent>(uid);
                        tally.ConsecutivePasses += 1;
                        if (tally.ConsecutivePasses >= _tallyMax)
                        {
                            Log.Info($"Deleting entity {uid} ({Name(uid)}) for inactivity.");
                            QueueDel(uid);
                        }
                    }
                }

                if (count >= _censusEntitiesPerFrame)
                    break;
            }

            if (count < _censusEntitiesPerFrame)
                _defaultEnumeratorValid = false;
        }

        if (!_ftlEnumeratorValid)
        {
            if (_timing.CurTime >= _nextFtlTime)
            {
                if (_ftlMapUid == EntityUid.Invalid)
                {
                    var ftlQuery = EntityQueryEnumerator<FTLMapComponent>();
                    while (ftlQuery.MoveNext(out var ftlUid, out _))
                    {
                        _ftlMapUid = ftlUid;
                        break;
                    }
                }

                if (TryComp(_ftlMapUid, out TransformComponent? xform))
                {
                    _ftlTransformSet.Clear();
                    _ftlTransformSet.EnsureCapacity(xform.ChildCount);
                    var enumerator = xform.ChildEnumerator;
                    while (enumerator.MoveNext(out var uid))
                    {
                        _ftlTransformSet.Add(uid);
                    }
                    _ftlEnumerator = _ftlTransformSet.GetEnumerator();
                    _ftlEnumeratorValid = true;
                }
                _nextFtlTime = _timing.CurTime + _censusPassPeriod;
            }
        }
        else
        {
            count = 0;
            while (_ftlEnumerator.MoveNext())
            {
                count++;
                var uid = _ftlEnumerator.Current;

                if (EntityManager.EntityExists(uid)
                    && TryComp(uid, out TransformComponent? xform)
                    && xform.ParentUid == _ftlMapUid
                    && (!_mindContainerQuery.TryComp(uid, out var mindContainer)
                    || !mindContainer.HasMind))
                {
                    var tally = EnsureComp<DeletionPassTallyComponent>(uid);
                    tally.ConsecutivePasses += 1;
                    if (tally.ConsecutivePasses >= _tallyMax)
                    {
                        QueueDel(uid);
                    }
                }

                if (count >= _censusEntitiesPerFrame)
                    break;
            }

            if (count < _censusEntitiesPerFrame)
                _ftlEnumeratorValid = false;
        }
    }
}
