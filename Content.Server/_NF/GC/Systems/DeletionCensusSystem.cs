// #define NF_CENSUS_DEBUG_LOG // Uncomment to enable debug logging

using Content.Server._NF.GC.Components;
using Content.Server.GameTicking;
using Content.Server.StationEvents.Events;
using Content.Server.Worldgen;
using Content.Server.Worldgen.Components;
using Content.Server.Worldgen.Systems;
using Content.Shared._NF.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Mind.Components;
using Content.Shared.Shuttles.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Server._NF.GC.Systems;

/// <summary>
/// A garbage collection system.
/// Deletes unused entities parented on the main map if they've been on an unloaded chunk for a given number of passes.
/// Each pass runs at a configurable period.
/// </summary>
public sealed class DeletionCensusSystem : EntitySystem
{
    // Dependencies
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly LinkedLifecycleGridSystem _linkedLifecycleGrid = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly WorldControllerSystem _world = default!;

    // Entity queries
    EntityQuery<DeletionCensusExemptComponent> _deletionCensusExemptQuery = default!;
    EntityQuery<LoadedChunkComponent> _loadedChunkQuery = default!;
    EntityQuery<MapGridComponent> _mapGridQuery = default!;
    EntityQuery<MindContainerComponent> _mindContainerQuery = default!;
    EntityQuery<WorldControllerComponent> _worldControllerQuery = default!;

    // These two will be cloned from the map's transform component at regular intervals.
    // Their children will be maintained between runs.
    private EntityUid _defaultMapUid;
    private EntityUid _ftlMapUid;
    private List<EntityUid> _defaultMapChildren = new();
    private List<EntityUid> _ftlMapChildren = new();
    private bool _defaultChildEnumeratorValid = false;
    private List<EntityUid>.Enumerator _defaultChildEnumerator = default!;
    private bool _ftlChildEnumeratorValid = false;
    private List<EntityUid>.Enumerator _ftlChildEnumerator = default!;
    private TimeSpan _nextDefaultCensusTime = TimeSpan.Zero;
    private TimeSpan _nextFtlCensusTime = TimeSpan.Zero;

    // GC parameters
    private bool _censusEnabled = true;
    private TimeSpan _censusPassPeriod = TimeSpan.FromMinutes(15);
    private int _censusEntitiesPerTick = 64;
    private int _censusTallyMax = 3; // The number of tallies needed before queueing an entity to be deleted.
    public override void Initialize()
    {
        base.Initialize();

        _deletionCensusExemptQuery = GetEntityQuery<DeletionCensusExemptComponent>();
        _loadedChunkQuery = GetEntityQuery<LoadedChunkComponent>();
        _mapGridQuery = GetEntityQuery<MapGridComponent>();
        _mindContainerQuery = GetEntityQuery<MindContainerComponent>();
        _worldControllerQuery = GetEntityQuery<WorldControllerComponent>();

        Subs.CVar(_cfg, NFCCVars.GarbageCollectionEnabled, SetGarbageCollectionEnabled, true);
        Subs.CVar(_cfg, NFCCVars.GarbageCollectionPeriod, SetGarbageCollectionPeriod, true);
        Subs.CVar(_cfg, NFCCVars.GarbageCollectionEntitiesPerTick, SetGarbageCollectionEntitiesPerTick, true);
        Subs.CVar(_cfg, NFCCVars.GarbageCollectionTally, SetGarbageCollectionTallyCount, true);

        // TODO: reset tally on reparent
        SubscribeLocalEvent<DeletionCensusTallyComponent, EntParentChangedMessage>(OnDeletionParentChanged);
        SubscribeLocalEvent<DeletionCensusExemptComponent, GridSplitEvent>(OnExemptGridSplit);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    #region CVar handlers
    private void SetGarbageCollectionEnabled(bool value)
    {
        _censusEnabled = value;
    }

    private void SetGarbageCollectionPeriod(int value)
    {
        if (value <= 0)
            return;

        _censusPassPeriod = TimeSpan.FromSeconds(value);

        // Ensure time until next census is within the requested period.
        var newNextCensusTime = _timing.CurTime + _censusPassPeriod;
        if (newNextCensusTime < _nextDefaultCensusTime)
            _nextDefaultCensusTime = newNextCensusTime;
        if (newNextCensusTime < _nextFtlCensusTime)
            _nextFtlCensusTime = newNextCensusTime;
    }

    private void SetGarbageCollectionEntitiesPerTick(int value)
    {
        _censusEntitiesPerTick = value;
    }

    private void SetGarbageCollectionTallyCount(int value)
    {
        _censusTallyMax = value;
    }
    #endregion

    #region Event handlers
    private void OnRoundRestart(RoundRestartCleanupEvent _)
    {
        _nextDefaultCensusTime = TimeSpan.Zero;
        _nextFtlCensusTime = TimeSpan.Zero;
        _defaultChildEnumerator = default!;
        _ftlChildEnumerator = default!;
        _defaultChildEnumeratorValid = false;
        _ftlChildEnumeratorValid = false;
        _defaultMapUid = EntityUid.Invalid;
        _ftlMapUid = EntityUid.Invalid;
    }

    private void OnDeletionParentChanged(Entity<DeletionCensusTallyComponent> ent, ref EntParentChangedMessage args)
    {
        RemComp<DeletionCensusTallyComponent>(ent);
    }

    private void OnExemptGridSplit(Entity<DeletionCensusExemptComponent> ent, ref GridSplitEvent args)
    {
        if (ent.Comp.PassOnGridSplit)
        {
            foreach (var grid in args.NewGrids)
            {
                var exemption = EnsureComp<DeletionCensusExemptComponent>(grid);
                exemption.PassOnGridSplit = true;
            }
        }
    }
    #endregion Event handlers

    #region Update
    public override void Update(float frameTime)
    {
        if (!_censusEnabled)
            return;

        if (!_defaultChildEnumeratorValid)
        {
            if (_timing.CurTime >= _nextDefaultCensusTime)
            {
                if (_defaultMapUid == EntityUid.Invalid && _map.TryGetMap(_gameTicker.DefaultMap, out var mapUid))
                    _defaultMapUid = mapUid.Value;

                if (TryComp(_defaultMapUid, out TransformComponent? xform))
                {
                    _defaultMapChildren.Clear();
                    _defaultMapChildren.EnsureCapacity(xform.ChildCount);
                    var enumerator = xform.ChildEnumerator;
                    while (enumerator.MoveNext(out var uid))
                    {
                        _defaultMapChildren.Add(uid);
                    }
                    _defaultChildEnumerator = _defaultMapChildren.GetEnumerator();
                    _defaultChildEnumeratorValid = true;
#if NF_CENSUS_DEBUG_LOG
                    Log.Info($"Default map census started.");
#endif
                }
                _nextDefaultCensusTime = _timing.CurTime + _censusPassPeriod;
#if NF_CENSUS_DEBUG_LOG
                Log.Info($"Next default census attempt at {_nextDefaultCensusTime}.");
#endif
            }
        }
        else
        {
            if (!_worldControllerQuery.TryComp(_defaultMapUid, out var worldController))
                _defaultChildEnumeratorValid = false;
            else
                _defaultChildEnumeratorValid = CheckNextDefaultEntities(_censusEntitiesPerTick, worldController);
        }

        if (!_ftlChildEnumeratorValid)
        {
            if (_timing.CurTime >= _nextFtlCensusTime)
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
                    _ftlMapChildren.Clear();
                    _ftlMapChildren.EnsureCapacity(xform.ChildCount);
                    var enumerator = xform.ChildEnumerator;
                    while (enumerator.MoveNext(out var uid))
                    {
                        _ftlMapChildren.Add(uid);
                    }
                    _ftlChildEnumerator = _ftlMapChildren.GetEnumerator();
                    _ftlChildEnumeratorValid = true;
#if NF_CENSUS_DEBUG_LOG
                    Log.Info($"FTL map census started.");
#endif
                }
                _nextFtlCensusTime = _timing.CurTime + _censusPassPeriod;
#if NF_CENSUS_DEBUG_LOG
                Log.Info($"Next FTL census attempt at {_nextDefaultCensusTime}.");
#endif
            }
        }
        else
        {
            _ftlChildEnumeratorValid = CheckNextFTLEntities(_censusEntitiesPerTick);
        }
    }

    /// <summary>
    /// Returns if this entity is exempt from the deletion census.
    /// </summary>
    /// <param name="uid">The entity to check.</param>
    /// <returns>If this entity is exempt from the deletion census.</returns>
    private bool IsExemptFromCensus(EntityUid uid)
    {
        if (_mindContainerQuery.TryComp(uid, out var mindContainer) && mindContainer.HasMind)
            return true;
        return _deletionCensusExemptQuery.HasComp(uid);
    }

    /// <summary>
    /// Checks the next few children of the default map to see if they should be removed.
    /// </summary>
    /// <param name="maxCount">The maximum number of entities to check.</param>
    /// <param name="worldController">The current world controller component of the default map.</param>
    /// <returns>If there are more entities to be enumerated in the list.</returns>
    private bool CheckNextDefaultEntities(int maxCount, WorldControllerComponent worldController)
    {
        int count = 0;
        while (_defaultChildEnumerator.MoveNext())
        {
            count++;
            var uid = _defaultChildEnumerator.Current;

            // Check if entity is excluded
            if (EntityManager.EntityExists(uid)
                && TryComp(uid, out TransformComponent? xform)
                && xform.ParentUid == _defaultMapUid
                && !IsExemptFromCensus(uid))
            {

                // Check chunk
                if (!_world.TryGetChunk(WorldGen.WorldToChunkCoords(_transform.GetWorldPosition(xform)).Floored(), _defaultMapUid, out var chunk, worldController)
                    || !_loadedChunkQuery.TryGetComponent(chunk, out var loaded)
                    || loaded.Loaders == null
                    || loaded.Loaders.Count == 0)
                {
                    var tally = EnsureComp<DeletionCensusTallyComponent>(uid);
                    tally.ConsecutivePasses += 1;
                    if (tally.ConsecutivePasses >= _censusTallyMax)
                    {
#if NF_CENSUS_DEBUG_LOG
                        Log.Info($"Deleting entity {uid} ({Name(uid)}) for inactivity.");
#endif
                        if (_mapGridQuery.HasComp(uid))
                            _linkedLifecycleGrid.UnparentPlayersFromGrid(uid, deleteGrid: true);
                        else
                            QueueDel(uid);
                    }
                }
            }

            if (count >= maxCount)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Checks the next few children of the FTL map to see if they should be removed.
    /// Non-exempt entities being parented on the FTL map at all is grounds for removal.
    /// </summary>
    /// <param name="maxCount">The maximum number of entities to check.</param>
    /// <returns>If there are more entities to be enumerated in the list.</returns>
    private bool CheckNextFTLEntities(int maxCount)
    {
        int count = 0;
        while (_ftlChildEnumerator.MoveNext())
        {
            count++;
            var uid = _ftlChildEnumerator.Current;

            // Check if entity is excluded
            if (EntityManager.EntityExists(uid)
                && TryComp(uid, out TransformComponent? xform)
                && xform.ParentUid == _ftlMapUid
                && !IsExemptFromCensus(uid))
            {
                var tally = EnsureComp<DeletionCensusTallyComponent>(uid);
                tally.ConsecutivePasses += 1;
                if (tally.ConsecutivePasses >= _censusTallyMax)
                {
                    QueueDel(uid);
                }
            }

            if (count >= maxCount)
                return false;
        }
        return true;
    }
    #endregion Update
}
