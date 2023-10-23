using System.Numerics;
using System.Threading.Tasks;
using Content.Server.Decals;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Shuttles.Events;
using Content.Shared.Decals;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Parallax.Biomes.Layers;
using Content.Shared.Parallax.Biomes.Markers;
using Microsoft.Extensions.ObjectPool;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Noise;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Threading;
using Robust.Shared.Utility;

namespace Content.Server.Parallax;

public sealed partial class BiomeSystem : SharedBiomeSystem
{
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IParallelManager _parallel = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ISerializationManager _serManager = default!;
    [Dependency] private readonly DecalSystem _decals = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly HashSet<EntityUid> _handledEntities = new();
    private const float DefaultLoadRange = 16f;
    private float _loadRange = DefaultLoadRange;

    private ObjectPool<HashSet<Vector2i>> _tilePool =
        new DefaultObjectPool<HashSet<Vector2i>>(new SetPolicy<Vector2i>(), 256);

    /// <summary>
    /// Load area for chunks containing tiles, decals etc.
    /// </summary>
    private Box2 _loadArea = new(-DefaultLoadRange, -DefaultLoadRange, DefaultLoadRange, DefaultLoadRange);

    /// <summary>
    /// Stores the chunks active for this tick temporarily.
    /// </summary>
    private readonly Dictionary<BiomeComponent, HashSet<Vector2i>> _activeChunks = new();

    private readonly Dictionary<BiomeComponent,
        Dictionary<string, HashSet<Vector2i>>> _markerChunks = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BiomeComponent, ComponentStartup>(OnBiomeStartup);
        SubscribeLocalEvent<BiomeComponent, MapInitEvent>(OnBiomeMapInit);
        SubscribeLocalEvent<FTLStartedEvent>(OnFTLStarted);
        SubscribeLocalEvent<ShuttleFlattenEvent>(OnShuttleFlatten);
        _configManager.OnValueChanged(CVars.NetMaxUpdateRange, SetLoadRange, true);
        InitializeCommands();
        _proto.PrototypesReloaded += ProtoReload;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _configManager.UnsubValueChanged(CVars.NetMaxUpdateRange, SetLoadRange);
        _proto.PrototypesReloaded -= ProtoReload;
    }

    private void ProtoReload(PrototypesReloadedEventArgs obj)
    {
        if (!obj.ByType.TryGetValue(typeof(BiomeTemplatePrototype), out var reloads))
            return;

        var query = AllEntityQuery<BiomeComponent>();

        while (query.MoveNext(out var biome))
        {
            if (biome.Template == null || !reloads.Modified.TryGetValue(biome.Template, out var proto))
                continue;

            SetTemplate(biome, (BiomeTemplatePrototype) proto);
        }
    }

    private void SetLoadRange(float obj)
    {
        // Round it up
        _loadRange = MathF.Ceiling(obj / ChunkSize) * ChunkSize;
        _loadArea = new Box2(-_loadRange, -_loadRange, _loadRange, _loadRange);
    }

    private void OnBiomeStartup(EntityUid uid, BiomeComponent component, ComponentStartup args)
    {
        component.Noise.SetSeed(component.Seed);
    }

    private void OnBiomeMapInit(EntityUid uid, BiomeComponent component, MapInitEvent args)
    {
        if (component.Seed != -1)
            return;

        SetSeed(component, _random.Next());
    }

    public void SetSeed(BiomeComponent component, int seed)
    {
        component.Seed = seed;
        component.Noise.SetSeed(seed);
        Dirty(component);
    }

    public void ClearTemplate(BiomeComponent component)
    {
        component.Layers.Clear();
        component.Template = null;
        Dirty(component);
    }

    /// <summary>
    /// Sets the <see cref="BiomeComponent.Template"/> and refreshes layers.
    /// </summary>
    public void SetTemplate(BiomeComponent component, BiomeTemplatePrototype template)
    {
        component.Layers.Clear();
        component.Template = template.ID;

        foreach (var layer in template.Layers)
        {
            component.Layers.Add(layer);
        }

        Dirty(component);
    }

    /// <summary>
    /// Adds the specified layer at the specified marker if it exists.
    /// </summary>
    public void AddLayer(BiomeComponent component, string id, IBiomeLayer addedLayer, int seedOffset = 0)
    {
        for (var i = 0; i < component.Layers.Count; i++)
        {
            var layer = component.Layers[i];

            if (layer is not BiomeDummyLayer dummy || dummy.ID != id)
                continue;

            addedLayer.Noise.SetSeed(addedLayer.Noise.GetSeed() + seedOffset);
            component.Layers.Insert(i, addedLayer);
            break;
        }

        Dirty(component);
    }

    public void AddMarkerLayer(BiomeComponent component, string marker)
    {
        if (!_proto.HasIndex<BiomeMarkerLayerPrototype>(marker))
        {
            // TODO: Log when we get a sawmill
            return;
        }

        component.MarkerLayers.Add(marker);
        Dirty(component);
    }

    /// <summary>
    /// Adds the specified template at the specified marker if it exists, withour overriding every layer.
    /// </summary>
    public void AddTemplate(BiomeComponent component, string id, BiomeTemplatePrototype template, int seedOffset = 0)
    {
        for (var i = 0; i < component.Layers.Count; i++)
        {
            var layer = component.Layers[i];

            if (layer is not BiomeDummyLayer dummy || dummy.ID != id)
                continue;

            for (var j = template.Layers.Count - 1; j >= 0; j--)
            {
                var addedLayer = template.Layers[j];
                addedLayer.Noise.SetSeed(addedLayer.Noise.GetSeed() + seedOffset);
                component.Layers.Insert(i, addedLayer);
            }

            break;
        }

        Dirty(component);
    }

    private void OnFTLStarted(ref FTLStartedEvent ev)
    {
        var targetMap = ev.TargetCoordinates.ToMap(EntityManager, _transform);
        var targetMapUid = _mapManager.GetMapEntityId(targetMap.MapId);

        if (!TryComp<BiomeComponent>(targetMapUid, out var biome))
            return;

        var preloadArea = new Vector2(32f, 32f);
        var targetArea = new Box2(targetMap.Position - preloadArea, targetMap.Position + preloadArea);
        Preload(targetMapUid, biome, targetArea);
    }

    private void OnShuttleFlatten(ref ShuttleFlattenEvent ev)
    {
        if (!TryComp<BiomeComponent>(ev.MapUid, out var biome) ||
            !TryComp<MapGridComponent>(ev.MapUid, out var grid))
        {
            return;
        }

        var tiles = new List<(Vector2i Index, Tile Tile)>();

        foreach (var aabb in ev.AABBs)
        {
            for (var x = Math.Floor(aabb.Left); x <= Math.Ceiling(aabb.Right); x++)
            {
                for (var y = Math.Floor(aabb.Bottom); y <= Math.Ceiling(aabb.Top); y++)
                {
                    var index = new Vector2i((int) x, (int) y);
                    var chunk = SharedMapSystem.GetChunkIndices(index, ChunkSize);

                    var mod = biome.ModifiedTiles.GetOrNew(chunk * ChunkSize);

                    if (!mod.Add(index) || !TryGetBiomeTile(index, biome.Layers, biome.Noise, grid, out var tile))
                        continue;

                    // If we flag it as modified then the tile is never set so need to do it ourselves.
                    tiles.Add((index, tile.Value));
                }
            }
        }

        grid.SetTiles(tiles);
    }

    /// <summary>
    /// Preloads biome for the specified area.
    /// </summary>
    public void Preload(EntityUid uid, BiomeComponent component, Box2 area)
    {
        var markers = component.MarkerLayers;
        var goobers = _markerChunks.GetOrNew(component);

        foreach (var layer in markers)
        {
            var proto = _proto.Index<BiomeMarkerLayerPrototype>(layer);
            var enumerator = new ChunkIndicesEnumerator(area, proto.Size);

            while (enumerator.MoveNext(out var chunk))
            {
                var chunkOrigin = chunk * proto.Size;
                var layerChunks = goobers.GetOrNew(proto.ID);
                layerChunks.Add(chunkOrigin.Value);
            }
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var biomeQuery = GetEntityQuery<BiomeComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();
        var biomes = AllEntityQuery<BiomeComponent>();

        while (biomes.MoveNext(out var biome))
        {
            _activeChunks.Add(biome, _tilePool.Get());
            _markerChunks.GetOrNew(biome);
        }

        // Get chunks in range
        foreach (var client in Filter.GetAllPlayers(_playerManager))
        {
            var pSession = (IPlayerSession) client;

            if (xformQuery.TryGetComponent(pSession.AttachedEntity, out var xform) &&
                _handledEntities.Add(pSession.AttachedEntity.Value) &&
                 biomeQuery.TryGetComponent(xform.MapUid, out var biome))
            {
                var worldPos = _transform.GetWorldPosition(xform, xformQuery);
                AddChunksInRange(biome, worldPos);

                foreach (var layer in biome.MarkerLayers)
                {
                    var layerProto = _proto.Index<BiomeMarkerLayerPrototype>(layer);
                    AddMarkerChunksInRange(biome, worldPos, layerProto);
                }
            }

            foreach (var viewer in pSession.ViewSubscriptions)
            {
                if (!_handledEntities.Add(viewer) ||
                    !xformQuery.TryGetComponent(viewer, out xform) ||
                    !biomeQuery.TryGetComponent(xform.MapUid, out biome))
                {
                    continue;
                }

                var worldPos = _transform.GetWorldPosition(xform, xformQuery);
                AddChunksInRange(biome, worldPos);

                foreach (var layer in biome.MarkerLayers)
                {
                    var layerProto = _proto.Index<BiomeMarkerLayerPrototype>(layer);
                    AddMarkerChunksInRange(biome, worldPos, layerProto);
                }
            }
        }

        var loadBiomes = AllEntityQuery<BiomeComponent, MapGridComponent>();

        while (loadBiomes.MoveNext(out var gridUid, out var biome, out var grid))
        {
            var noise = biome.Noise;

            // Load new chunks
            LoadChunks(biome, gridUid, grid, noise, xformQuery);
            // Unload old chunks
            UnloadChunks(biome, gridUid, grid, noise);
        }

        _handledEntities.Clear();

        foreach (var tiles in _activeChunks.Values)
        {
            _tilePool.Return(tiles);
        }

        _activeChunks.Clear();
        _markerChunks.Clear();
    }

    private void AddChunksInRange(BiomeComponent biome, Vector2 worldPos)
    {
        var enumerator = new ChunkIndicesEnumerator(_loadArea.Translated(worldPos), ChunkSize);

        while (enumerator.MoveNext(out var chunkOrigin))
        {
            _activeChunks[biome].Add(chunkOrigin.Value * ChunkSize);
        }
    }

    private void AddMarkerChunksInRange(BiomeComponent biome, Vector2 worldPos, IBiomeMarkerLayer layer)
    {
        // Offset the load area so it's centralised.
        var loadArea = new Box2(0, 0, layer.Size, layer.Size);
        var halfLayer = new Vector2(layer.Size / 2f);

        var enumerator = new ChunkIndicesEnumerator(loadArea.Translated(worldPos - halfLayer), layer.Size);

        while (enumerator.MoveNext(out var chunkOrigin))
        {
            var lay = _markerChunks[biome].GetOrNew(layer.ID);
            lay.Add(chunkOrigin.Value * layer.Size);
        }
    }

    #region Load

    /// <summary>
    /// Loads all of the chunks for a particular biome, as well as handle any marker chunks.
    /// </summary>
    private void LoadChunks(
        BiomeComponent component,
        EntityUid gridUid,
        MapGridComponent grid,
        FastNoiseLite noise,
        EntityQuery<TransformComponent> xformQuery)
    {
        var markers = _markerChunks[component];
        var loadedMarkers = component.LoadedMarkers;

        foreach (var (layer, chunks) in markers)
        {
            Parallel.ForEach(chunks, new ParallelOptions() { MaxDegreeOfParallelism = _parallel.ParallelProcessCount }, chunk =>
            {
                if (loadedMarkers.TryGetValue(layer, out var mobChunks) && mobChunks.Contains(chunk))
                    return;

                var noiseCopy = new FastNoiseLite();
                _serManager.CopyTo(component.Noise, ref noiseCopy, notNullableOverride: true);
                var spawnSet = _tilePool.Get();
                var frontier = new ValueList<Vector2i>(32);

                // Make a temporary version and copy back in later.
                var pending = new Dictionary<Vector2i, Dictionary<string, List<Vector2i>>>();

                var layerProto = _proto.Index<BiomeMarkerLayerPrototype>(layer);
                var buffer = layerProto.Radius / 2f;
                var rand = new Random(noise.GetSeed() + chunk.X * 8 + chunk.Y + layerProto.GetHashCode());

                // We treat a null entity mask as requiring nothing else on the tile
                var lower = (int) Math.Floor(buffer);
                var upper = (int) Math.Ceiling(layerProto.Size - buffer);

                // TODO: Need poisson but crashes whenever I use moony's due to inputs or smth idk
                var count = (int) ((layerProto.Size - buffer) * (layerProto.Size - buffer) /
                                   (layerProto.Radius * layerProto.Radius));
                count = Math.Min(count, layerProto.MaxCount);

                // Pick a random tile then BFS outwards from it
                // It will bias edge tiles significantly more but will make the CPU cry less.
                for (var i = 0; i < count; i++)
                {
                    var groupCount = layerProto.GroupCount;
                    var startNodeX = rand.Next(lower, upper + 1);
                    var startNodeY = rand.Next(lower, upper + 1);
                    var startNode = new Vector2i(startNodeX, startNodeY);
                    frontier.Clear();
                    frontier.Add(startNode + chunk);

                    while (groupCount > 0 && frontier.Count > 0)
                    {
                        var frontierIndex = rand.Next(frontier.Count);
                        var node = frontier[frontierIndex];
                        frontier.RemoveSwap(frontierIndex);

                        // Add neighbors regardless.
                        for (var x = -1; x <= 1; x++)
                        {
                            for (var y = -1; y <= 1; y++)
                            {
                                if (x != 0 && y != 0)
                                    continue;

                                var neighbor = new Vector2i(x + node.X, y + node.Y);
                                var chunkOffset = neighbor - chunk;

                                // Check if it's inbounds.
                                if (chunkOffset.X < lower ||
                                    chunkOffset.Y < lower ||
                                    chunkOffset.X > upper ||
                                    chunkOffset.Y > upper)
                                {
                                    continue;
                                }

                                if (!spawnSet.Add(neighbor))
                                    continue;

                                frontier.Add(neighbor);
                            }
                        }

                        // Check if it's a valid spawn, if so then use it.
                        var enumerator = grid.GetAnchoredEntitiesEnumerator(node);

                        if (enumerator.MoveNext(out _))
                            continue;

                        // Check if mask matches.
                        TryGetEntity(node, component.Layers, noiseCopy, grid, out var proto);

                        if (proto != layerProto.EntityMask)
                        {
                            continue;
                        }

                        var chunkOrigin = SharedMapSystem.GetChunkIndices(node, ChunkSize) * ChunkSize;

                        if (!pending.TryGetValue(chunkOrigin, out var pendingMarkers))
                        {
                            pendingMarkers = new Dictionary<string, List<Vector2i>>();
                            pending[chunkOrigin] = pendingMarkers;
                        }

                        if (!pendingMarkers.TryGetValue(layer, out var layerMarkers))
                        {
                            layerMarkers = new List<Vector2i>();
                            pendingMarkers[layer] = layerMarkers;
                        }

                        // Log.Info($"Added node at {actualNode} for chunk {chunkOrigin}");
                        layerMarkers.Add(node);
                        groupCount--;
                    }
                }

                lock (component.PendingMarkers)
                {
                    if (!loadedMarkers.TryGetValue(layer, out var lockMobChunks))
                    {
                        lockMobChunks = new HashSet<Vector2i>();
                        loadedMarkers[layer] = lockMobChunks;
                    }

                    lockMobChunks.Add(chunk);

                    foreach (var (chunkOrigin, layers) in pending)
                    {
                        if (!component.PendingMarkers.TryGetValue(chunkOrigin, out var lockMarkers))
                        {
                            lockMarkers = new Dictionary<string, List<Vector2i>>();
                            component.PendingMarkers[chunkOrigin] = lockMarkers;
                        }

                        foreach (var (lockLayer, nodes) in layers)
                        {
                            lockMarkers[lockLayer] = nodes;
                        }
                    }

                    _tilePool.Return(spawnSet);
                }
            });
        }

        var active = _activeChunks[component];
        List<(Vector2i, Tile)>? tiles = null;

        foreach (var chunk in active)
        {
            if (!component.LoadedChunks.Add(chunk))
                continue;

            tiles ??= new List<(Vector2i, Tile)>(ChunkSize * ChunkSize);
            // Load NOW!
            LoadChunk(component, gridUid, grid, chunk, noise, tiles, xformQuery);
        }
    }

    /// <summary>
    /// Loads a particular queued chunk for a biome.
    /// </summary>
    private void LoadChunk(
        BiomeComponent component,
        EntityUid gridUid,
        MapGridComponent grid,
        Vector2i chunk,
        FastNoiseLite noise,
        List<(Vector2i, Tile)> tiles,
        EntityQuery<TransformComponent> xformQuery)
    {
        component.ModifiedTiles.TryGetValue(chunk, out var modified);
        modified ??= _tilePool.Get();

        // Load any pending marker tiles first.
        if (component.PendingMarkers.TryGetValue(chunk, out var layers))
        {
            foreach (var (layer, nodes) in layers)
            {
                var layerProto = _proto.Index<BiomeMarkerLayerPrototype>(layer);

                foreach (var node in nodes)
                {
                    if (modified.Contains(node))
                        continue;

                    // Need to ensure the tile under it has loaded for anchoring.
                    if (TryGetBiomeTile(node, component.Layers, component.Noise, grid, out var tile))
                    {
                        grid.SetTile(node, tile.Value);
                    }

                    // If it is a ghost role then purge it
                    // TODO: This is *kind* of a bandaid but natural mobs spawns needs a lot more work.
                    // Ideally we'd just have ghost role and non-ghost role variants for some stuff.
                    var uid = EntityManager.CreateEntityUninitialized(layerProto.Prototype, grid.GridTileToLocal(node));
                    RemComp<GhostTakeoverAvailableComponent>(uid);
                    RemComp<GhostRoleComponent>(uid);
                    EntityManager.InitializeAndStartEntity(uid);
                    modified.Add(node);
                }
            }

            component.PendingMarkers.Remove(chunk);
        }

        // Set tiles first
        for (var x = 0; x < ChunkSize; x++)
        {
            for (var y = 0; y < ChunkSize; y++)
            {
                var indices = new Vector2i(x + chunk.X, y + chunk.Y);

                if (modified.Contains(indices))
                    continue;

                // If there's existing data then don't overwrite it.
                if (grid.TryGetTileRef(indices, out var tileRef) && !tileRef.Tile.IsEmpty)
                    continue;

                // Pass in null so we don't try to get the tileref.
                if (!TryGetBiomeTile(indices, component.Layers, noise, null, out var biomeTile) || biomeTile.Value == tileRef.Tile)
                    continue;

                tiles.Add((indices, biomeTile.Value));
            }
        }

        grid.SetTiles(tiles);
        tiles.Clear();

        // Now do entities
        var loadedEntities = new Dictionary<EntityUid, Vector2i>();
        component.LoadedEntities.Add(chunk, loadedEntities);

        for (var x = 0; x < ChunkSize; x++)
        {
            for (var y = 0; y < ChunkSize; y++)
            {
                var indices = new Vector2i(x + chunk.X, y + chunk.Y);

                if (modified.Contains(indices))
                    continue;

                // Don't mess with anything that's potentially anchored.
                var anchored = grid.GetAnchoredEntitiesEnumerator(indices);

                if (anchored.MoveNext(out _) || !TryGetEntity(indices, component.Layers, noise, grid, out var entPrototype))
                    continue;

                // TODO: Fix non-anchored ents spawning.
                // Just track loaded chunks for now.
                var ent = Spawn(entPrototype, grid.GridTileToLocal(indices));

                // At least for now unless we do lookups or smth, only work with anchoring.
                if (xformQuery.TryGetComponent(ent, out var xform) && !xform.Anchored)
                {
                    _transform.AnchorEntity(ent, xform, gridUid, grid, indices);
                }

                loadedEntities.Add(ent, indices);
            }
        }

        // Decals
        var loadedDecals = new Dictionary<uint, Vector2i>();
        component.LoadedDecals.Add(chunk, loadedDecals);

        for (var x = 0; x < ChunkSize; x++)
        {
            for (var y = 0; y < ChunkSize; y++)
            {
                var indices = new Vector2i(x + chunk.X, y + chunk.Y);

                if (modified.Contains(indices))
                    continue;

                // Don't mess with anything that's potentially anchored.
                var anchored = grid.GetAnchoredEntitiesEnumerator(indices);

                if (anchored.MoveNext(out _) || !TryGetDecals(indices, component.Layers, noise, grid, out var decals))
                    continue;

                foreach (var decal in decals)
                {
                    if (!_decals.TryAddDecal(decal.ID, new EntityCoordinates(gridUid, decal.Position), out var dec))
                        continue;

                    loadedDecals.Add(dec, indices);
                }
            }
        }

        if (modified.Count == 0)
        {
            _tilePool.Return(modified);
            component.ModifiedTiles.Remove(chunk);
        }
        else
        {
            component.ModifiedTiles[chunk] = modified;
        }
    }

    #endregion

    #region Unload

    /// <summary>
    /// Handles all of the queued chunk unloads for a particular biome.
    /// </summary>
    private void UnloadChunks(BiomeComponent component, EntityUid gridUid, MapGridComponent grid, FastNoiseLite noise)
    {
        var active = _activeChunks[component];
        List<(Vector2i, Tile)>? tiles = null;

        foreach (var chunk in component.LoadedChunks)
        {
            if (active.Contains(chunk) || !component.LoadedChunks.Remove(chunk))
                continue;

            // Unload NOW!
            tiles ??= new List<(Vector2i, Tile)>(ChunkSize * ChunkSize);
            UnloadChunk(component, gridUid, grid, chunk, noise, tiles);
        }
    }

    /// <summary>
    /// Unloads a specific biome chunk.
    /// </summary>
    private void UnloadChunk(BiomeComponent component, EntityUid gridUid, MapGridComponent grid, Vector2i chunk, FastNoiseLite noise, List<(Vector2i, Tile)> tiles)
    {
        // Reverse order to loading
        component.ModifiedTiles.TryGetValue(chunk, out var modified);
        modified ??= new HashSet<Vector2i>();

        // Delete decals
        foreach (var (dec, indices) in component.LoadedDecals[chunk])
        {
            // If we couldn't remove it then flag the tile to never be touched.
            if (!_decals.RemoveDecal(gridUid, dec))
            {
                modified.Add(indices);
            }
        }

        component.LoadedDecals.Remove(chunk);

        // Delete entities
        // Ideally any entities that aren't modified just get deleted and re-generated later
        // This is because if we want to save the map (e.g. persistent server) it makes the file much smaller
        // and also if the map is enormous will make stuff like physics broadphase much faster
        var xformQuery = GetEntityQuery<TransformComponent>();

        foreach (var (ent, tile) in component.LoadedEntities[chunk])
        {
            if (Deleted(ent) || !xformQuery.TryGetComponent(ent, out var xform))
            {
                modified.Add(tile);
                continue;
            }

            // It's moved
            var entTile = grid.LocalToTile(xform.Coordinates);

            if (!xform.Anchored || entTile != tile)
            {
                modified.Add(tile);
                continue;
            }

            if (!EntityManager.IsDefault(ent))
            {
                modified.Add(tile);
                continue;
            }

            Del(ent);
        }

        component.LoadedEntities.Remove(chunk);

        // Unset tiles (if the data is custom)

        for (var x = 0; x < ChunkSize; x++)
        {
            for (var y = 0; y < ChunkSize; y++)
            {
                var indices = new Vector2i(x + chunk.X, y + chunk.Y);

                if (modified.Contains(indices))
                    continue;

                // Don't mess with anything that's potentially anchored.
                var anchored = grid.GetAnchoredEntitiesEnumerator(indices);

                if (anchored.MoveNext(out _))
                {
                    modified.Add(indices);
                    continue;
                }

                // If it's default data unload the tile.
                if (!TryGetBiomeTile(indices, component.Layers, noise, null, out var biomeTile) ||
                    grid.TryGetTileRef(indices, out var tileRef) && tileRef.Tile != biomeTile.Value)
                {
                    modified.Add(indices);
                    continue;
                }

                tiles.Add((indices, Tile.Empty));
            }
        }

        grid.SetTiles(tiles);
        tiles.Clear();
        component.LoadedChunks.Remove(chunk);

        if (modified.Count == 0)
        {
            component.ModifiedTiles.Remove(chunk);
        }
        else
        {
            component.ModifiedTiles[chunk] = modified;
        }
    }

    #endregion
}
