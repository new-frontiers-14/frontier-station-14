using System.Linq;
using System.Threading;
using Content.Server.Salvage.Expeditions;
using Content.Server.Salvage.Expeditions.Structure;
using Content.Shared.CCVar;
using Content.Shared.Examine;
using Content.Shared.Random.Helpers;
using Content.Shared.Salvage.Expeditions;
using Robust.Shared.Audio;
using Robust.Shared.CPUJob.JobQueues;
using Robust.Shared.CPUJob.JobQueues.Queues;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Content.Server._NF.Salvage.Expeditions; // Frontier
using Content.Server.Station.Components; // Frontier
using Content.Shared.Procedural; // Frontier
using Content.Shared.Salvage; // Frontier
using Robust.Shared.Prototypes; // Frontier
using Content.Shared._NF.CCVar; // Frontier
using Content.Shared.Shuttles.Components; // Frontier
using Robust.Shared.Configuration;
using Content.Shared.Ghost;
using System.Numerics; // Frontier

namespace Content.Server.Salvage;

public sealed partial class SalvageSystem
{
    /*
     * Handles setup / teardown of salvage expeditions.
     */

    private const int MissionLimit = 5; // Frontier: 3<5

    private readonly JobQueue _salvageQueue = new();
    private readonly List<(SpawnSalvageMissionJob Job, CancellationTokenSource CancelToken)> _salvageJobs = new();
    private const double SalvageJobTime = 0.002;
    private readonly List<(ProtoId<SalvageDifficultyPrototype> id, int value)> _missionDifficulties = [("NFModerate", 0), ("NFHazardous", 1), ("NFExtreme", 2)]; // Frontier: mission difficulties with order

    [Dependency] private readonly IConfigurationManager _cfgManager = default!; // Frontier

    private float _cooldown;
    private float _failedCooldown; // Frontier
    public float TravelTime { get; private set; } // Frontier
    public bool ProximityCheck { get; private set; } // Frontier

    private void InitializeExpeditions()
    {
        SubscribeLocalEvent<SalvageExpeditionConsoleComponent, ComponentInit>(OnSalvageConsoleInit);
        SubscribeLocalEvent<SalvageExpeditionConsoleComponent, EntParentChangedMessage>(OnSalvageConsoleParent);
        SubscribeLocalEvent<SalvageExpeditionConsoleComponent, ClaimSalvageMessage>(OnSalvageClaimMessage);
        SubscribeLocalEvent<SalvageExpeditionDataComponent, ExpeditionSpawnCompleteEvent>(OnExpeditionSpawnComplete); // Frontier: more gracefully handle expedition generation failures
        SubscribeLocalEvent<SalvageExpeditionConsoleComponent, FinishSalvageMessage>(OnSalvageFinishMessage); // Frontier: For early finish

        SubscribeLocalEvent<SalvageExpeditionComponent, MapInitEvent>(OnExpeditionMapInit);
        SubscribeLocalEvent<SalvageExpeditionComponent, ComponentShutdown>(OnExpeditionShutdown);
        SubscribeLocalEvent<SalvageExpeditionComponent, ComponentGetState>(OnExpeditionGetState);
        SubscribeLocalEvent<SalvageExpeditionComponent, EntityTerminatingEvent>(OnMapTerminating); // Frontier

        SubscribeLocalEvent<SalvageStructureComponent, ExaminedEvent>(OnStructureExamine);

        _cooldown = _cfgManager.GetCVar(CCVars.SalvageExpeditionCooldown);
        Subs.CVar(_cfgManager, CCVars.SalvageExpeditionCooldown, SetCooldownChange);
        _failedCooldown = _cfgManager.GetCVar(NFCCVars.SalvageExpeditionFailedCooldown); // Frontier
        Subs.CVar(_cfgManager, NFCCVars.SalvageExpeditionFailedCooldown, SetFailedCooldownChange); // Frontier
        TravelTime = _cfgManager.GetCVar(NFCCVars.SalvageExpeditionTravelTime); // Frontier
        Subs.CVar(_cfgManager, NFCCVars.SalvageExpeditionTravelTime, SetTravelTime); // Frontier
        ProximityCheck = _cfgManager.GetCVar(NFCCVars.SalvageExpeditionProximityCheck); // Frontier
        Subs.CVar(_cfgManager, NFCCVars.SalvageExpeditionProximityCheck, SetProximityCheck); // Frontier
    }

    private void OnExpeditionGetState(EntityUid uid, SalvageExpeditionComponent component, ref ComponentGetState args)
    {
        args.State = new SalvageExpeditionComponentState()
        {
            Stage = component.Stage,
            SelectedSong = component.SelectedSong // Frontier: note, not dirtied on map init (not needed)
        };
    }

    private void SetCooldownChange(float obj)
    {
        // Update the active cooldowns if we change it.
        var diff = obj - _cooldown;

        var query = AllEntityQuery<SalvageExpeditionDataComponent>();

        while (query.MoveNext(out var comp))
        {
            comp.NextOffer += TimeSpan.FromSeconds(diff);
        }

        _cooldown = obj;
    }

    // Frontier: failed cooldowns
    private void SetFailedCooldownChange(float obj)
    {
        // Note: we don't know whether or not players have failed missions, so let's not punish/reward them if this gets changed.
        _failedCooldown = obj;
    }

    private void SetTravelTime(float obj)
    {
        TravelTime = obj;
    }

    private void SetProximityCheck(bool obj)
    {
        ProximityCheck = obj;
    }
    // End Frontier

    private void OnExpeditionMapInit(EntityUid uid, SalvageExpeditionComponent component, MapInitEvent args)
    {
        component.SelectedSong = _audio.ResolveSound(component.Sound);
    }

    private void OnExpeditionShutdown(EntityUid uid, SalvageExpeditionComponent component, ComponentShutdown args)
    {
        // component.Stream = _audio.Stop(component.Stream); // Frontier: moved to client

        foreach (var (job, cancelToken) in _salvageJobs.ToArray())
        {
            if (job.Station == component.Station)
            {
                cancelToken.Cancel();
                _salvageJobs.Remove((job, cancelToken));
            }
        }

        if (Deleted(component.Station))
            return;

        // Finish mission
        if (TryComp<SalvageExpeditionDataComponent>(component.Station, out var data))
        {
            FinishExpedition((component.Station, data), component, uid); // Frontier: add component
        }
    }

    private void UpdateExpeditions()
    {
        var currentTime = _timing.CurTime;
        _salvageQueue.Process();

        foreach (var (job, cancelToken) in _salvageJobs.ToArray())
        {
            switch (job.Status)
            {
                case JobStatus.Finished:
                    _salvageJobs.Remove((job, cancelToken));
                    break;
            }
        }

        var query = EntityQueryEnumerator<SalvageExpeditionDataComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            // Update offers
            if (comp.NextOffer > currentTime || comp.Claimed)
                continue;

            // Frontier: disable cooldown when still in FTL
            if (!TryComp<StationDataComponent>(uid, out var stationData)
                || !HasComp<FTLComponent>(_station.GetLargestGrid(stationData)))
            {
                comp.Cooldown = false;
            }
            // End Frontier: disable cooldown when still in FTL
            // comp.NextOffer += TimeSpan.FromSeconds(_cooldown); // Frontier
            comp.NextOffer = currentTime + TimeSpan.FromSeconds(_cooldown); // Frontier
            comp.CooldownTime = TimeSpan.FromSeconds(_cooldown); // Frontier
            GenerateMissions(comp);
            UpdateConsoles((uid, comp));
        }
    }

    private void FinishExpedition(Entity<SalvageExpeditionDataComponent> expedition, SalvageExpeditionComponent expeditionComp, EntityUid uid)
    {
        var component = expedition.Comp;
        // Frontier: separate timeout/announcement for success/failures
        if (expeditionComp.Completed)
        {
            component.NextOffer = _timing.CurTime + TimeSpan.FromSeconds(_cooldown);
            component.CooldownTime = TimeSpan.FromSeconds(_cooldown);
            Announce(uid, Loc.GetString("salvage-expedition-completed"));
        }
        else
        {
            component.NextOffer = _timing.CurTime + TimeSpan.FromSeconds(_failedCooldown);
            component.CooldownTime = TimeSpan.FromSeconds(_failedCooldown);
            Announce(uid, Loc.GetString("salvage-expedition-failed"));
        }
        // End Frontier: separate timeout/announcement for success/failures
        component.ActiveMission = 0;
        component.Cooldown = true;
        UpdateConsoles(expedition);
    }

    private void GenerateMissions(SalvageExpeditionDataComponent component)
    {
        component.Missions.Clear();

        // Frontier: generate missions from an arbitrary set of difficulties
        if (_missionDifficulties.Count <= 0)
        {
            Log.Error("No expedition mission difficulties to pick from!");
            return;
        }

        // this doesn't support having more missions than types of ratings
        // but the previous system didn't do that either.
        var allDifficulties = _missionDifficulties; // Frontier: Enum.GetValues<DifficultyRating>() < _missionDifficulties
        _random.Shuffle(allDifficulties);
        var difficulties = allDifficulties.Take(MissionLimit).ToList();

        // If we support more missions than there are accepted types, pick more until you're up to MissionLimit
        while (difficulties.Count < MissionLimit)
        {
            var difficultyIndex = _random.Next(_missionDifficulties.Count);
            difficulties.Add(_missionDifficulties[difficultyIndex]);
        }
        difficulties.Sort((x, y) => { return Comparer<int>.Default.Compare(x.value, y.value); });

        for (var i = 0; i < MissionLimit; i++)
        {
            var mission = new SalvageMissionParams
            {
                Index = component.NextIndex,
                MissionType = (SalvageMissionType)_random.NextByte((byte)SalvageMissionType.Max + 1), // Frontier
                Seed = _random.Next(),
                Difficulty = difficulties[i].id,
            };

            component.Missions[component.NextIndex++] = mission;
        }
        // End Frontier: generate missions from an arbitrary set of difficulties
    }

    private SalvageExpeditionConsoleState GetState(SalvageExpeditionDataComponent component)
    {
        var missions = component.Missions.Values.ToList();
        return new SalvageExpeditionConsoleState(component.NextOffer, component.Claimed, component.Cooldown, component.ActiveMission, missions, component.CanFinish, component.CooldownTime); // Frontier: add CanFinish, CooldownTime
    }

    private void SpawnMission(SalvageMissionParams missionParams, EntityUid station, EntityUid? coordinatesDisk)
    {
        var cancelToken = new CancellationTokenSource();
        var job = new SpawnSalvageMissionJob(
            SalvageJobTime,
            EntityManager,
            _timing,
            _logManager,
            _prototypeManager,
            _anchorable,
            _biome,
            _dungeon,
            _metaData,
            _mapSystem,
            _station, // Frontier
            _shuttle, // Frontier
            this, // Frontier
            station,
            coordinatesDisk,
            missionParams,
            cancelToken.Token);

        _salvageJobs.Add((job, cancelToken));
        _salvageQueue.EnqueueJob(job);
    }

    private void OnStructureExamine(EntityUid uid, SalvageStructureComponent component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("salvage-expedition-structure-examine"));
    }

    // Frontier: exped job handling, ghost reparenting
    // Handle exped spawn job failures gracefully - reset the console
    private void OnExpeditionSpawnComplete(EntityUid uid, SalvageExpeditionDataComponent component, ExpeditionSpawnCompleteEvent ev)
    {
        if (component.ActiveMission == ev.MissionIndex && !ev.Success)
        {
            component.ActiveMission = 0;
            component.Cooldown = false;
            UpdateConsoles((uid, component));
        }
    }

    // Send all ghosts (relevant for admins) back to the default map so they don't lose their stuff.
    private void OnMapTerminating(EntityUid uid, SalvageExpeditionComponent component, EntityTerminatingEvent ev)
    {
        var ghosts = EntityQueryEnumerator<GhostComponent, TransformComponent>();
        var newCoords = new MapCoordinates(Vector2.Zero, _gameTicker.DefaultMap);
        while (ghosts.MoveNext(out var ghostUid, out _, out var xform))
        {
            if (xform.MapUid == uid)
                _transform.SetMapCoordinates(ghostUid, newCoords);
        }
    }
    // End Frontier
}
