using System.Threading;
using System.Threading.Tasks;
using Content.Server.Construction;
using Robust.Shared.CPUJob.JobQueues.Queues;
using Content.Server.Decals;
using Content.Server.GameTicking.Events;
using Content.Shared.CCVar;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Procedural;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Procedural;

public sealed partial class DungeonSystem : SharedDungeonSystem
{
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;
    [Dependency] private readonly AnchorableSystem _anchorable = default!;
    [Dependency] private readonly DecalSystem _decals = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly TileSystem _tile = default!;
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private ISawmill _sawmill = default!;

    private const double DungeonJobTime = 0.005;

    public const int CollisionMask = (int) CollisionGroup.Impassable;
    public const int CollisionLayer = (int) CollisionGroup.Impassable;

    private readonly JobQueue _dungeonJobQueue = new(DungeonJobTime);
    private readonly Dictionary<DungeonJob, CancellationTokenSource> _dungeonJobs = new();

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = Logger.GetSawmill("dungen");
        _console.RegisterCommand("dungen", Loc.GetString("cmd-dungen-desc"), Loc.GetString("cmd-dungen-help"), GenerateDungeon, CompletionCallback);
        _console.RegisterCommand("dungen_preset_vis", Loc.GetString("cmd-dungen_preset_vis-desc"), Loc.GetString("cmd-dungen_preset_vis-help"), DungeonPresetVis, PresetCallback);
        _console.RegisterCommand("dungen_pack_vis", Loc.GetString("cmd-dungen_pack_vis-desc"), Loc.GetString("cmd-dungen_pack_vis-help"), DungeonPackVis, PackCallback);
        _prototype.PrototypesReloaded += PrototypeReload;
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _dungeonJobQueue.Process();
    }

    private void OnRoundStart(RoundStartingEvent ev)
    {
        foreach (var token in _dungeonJobs.Values)
        {
            token.Cancel();
        }

        _dungeonJobs.Clear();
        var query = AllEntityQuery<DungeonAtlasTemplateComponent>();

        while (query.MoveNext(out var uid, out _))
        {
            QueueDel(uid);
        }

        if (!_configManager.GetCVar(CCVars.ProcgenPreload))
            return;

        // Force all templates to be setup.
        foreach (var room in _prototype.EnumeratePrototypes<DungeonRoomPrototype>())
        {
            GetOrCreateTemplate(room);
        }
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _prototype.PrototypesReloaded -= PrototypeReload;

        foreach (var token in _dungeonJobs.Values)
        {
            token.Cancel();
        }

        _dungeonJobs.Clear();
    }

    private void PrototypeReload(PrototypesReloadedEventArgs obj)
    {
        if (!obj.ByType.TryGetValue(typeof(DungeonRoomPrototype), out var rooms))
        {
            return;
        }

        foreach (var proto in rooms.Modified.Values)
        {
            var roomProto = (DungeonRoomPrototype) proto;
            var query = AllEntityQuery<DungeonAtlasTemplateComponent>();

            while (query.MoveNext(out var uid, out var comp))
            {
                if (!roomProto.AtlasPath.Equals(comp.Path))
                    continue;

                QueueDel(uid);
                break;
            }
        }

        if (!_configManager.GetCVar(CCVars.ProcgenPreload))
            return;

        foreach (var proto in rooms.Modified.Values)
        {
            var roomProto = (DungeonRoomPrototype) proto;
            var query = AllEntityQuery<DungeonAtlasTemplateComponent>();
            var found = false;

            while (query.MoveNext(out var comp))
            {
                if (!roomProto.AtlasPath.Equals(comp.Path))
                    continue;

                found = true;
                break;
            }

            if (!found)
            {
                GetOrCreateTemplate(roomProto);
            }
        }
    }

    public MapId GetOrCreateTemplate(DungeonRoomPrototype proto)
    {
        var query = AllEntityQuery<DungeonAtlasTemplateComponent>();
        DungeonAtlasTemplateComponent? comp;

        while (query.MoveNext(out var uid, out comp))
        {
            // Exists
            if (comp.Path.Equals(proto.AtlasPath))
                return Transform(uid).MapID;
        }

        var mapId = _mapManager.CreateMap();
        _mapManager.AddUninitializedMap(mapId);
        _loader.Load(mapId, proto.AtlasPath.ToString());
        var mapUid = _mapManager.GetMapEntityId(mapId);
        _mapManager.SetMapPaused(mapId, true);
        comp = AddComp<DungeonAtlasTemplateComponent>(mapUid);
        comp.Path = proto.AtlasPath;
        return mapId;
    }

    public void GenerateDungeon(DungeonConfigPrototype gen,
        EntityUid gridUid,
        MapGridComponent grid,
        Vector2i position,
        int seed)
    {
        var cancelToken = new CancellationTokenSource();
        var job = new DungeonJob(
            _sawmill,
            DungeonJobTime,
            EntityManager,
            _mapManager,
            _prototype,
            _tileDefManager,
            _anchorable,
            _decals,
            this,
            _lookup,
            _tag,
            _tile,
            _transform,
            gen,
            grid,
            gridUid,
            seed,
            position,
            cancelToken.Token);

        _dungeonJobs.Add(job, cancelToken);
        _dungeonJobQueue.EnqueueJob(job);
        job.Run();
    }

    public async Task<Dungeon> GenerateDungeonAsync(
        DungeonConfigPrototype gen,
        EntityUid gridUid,
        MapGridComponent grid,
        Vector2i position,
        int seed)
    {
        var cancelToken = new CancellationTokenSource();
        var job = new DungeonJob(
            _sawmill,
            DungeonJobTime,
            EntityManager,
            _mapManager,
            _prototype,
            _tileDefManager,
            _anchorable,
            _decals,
            this,
            _lookup,
            _tag,
            _tile,
            _transform,
            gen,
            grid,
            gridUid,
            seed,
            position,
            cancelToken.Token);

        _dungeonJobs.Add(job, cancelToken);
        _dungeonJobQueue.EnqueueJob(job);
        job.Run();
        await job.AsTask;

        if (job.Exception != null)
        {
            throw job.Exception;
        }

        return job.Result!;
    }

    public Angle GetDungeonRotation(int seed)
    {
        // Mask 0 | 1 for rotation seed
        var dungeonRotationSeed = 3 & seed;
        return Math.PI / 2 * dungeonRotationSeed;
    }
}
