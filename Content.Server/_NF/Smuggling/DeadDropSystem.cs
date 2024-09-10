using System.Linq;
using System.Text;
using Content.Server._NF.GameTicking.Events;
using Content.Server._NF.SectorServices;
using Content.Server._NF.Smuggling.Components;
using Content.Server.Administration.Logs;
using Content.Server.Radio.EntitySystems;
using Content.Server.Shipyard.Systems;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Database;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Paper;
using Content.Shared.Radio;
using Content.Shared.Shuttles.Components;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._NF.Smuggling;

public sealed class DeadDropSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ShipyardSystem _shipyard = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedMapSystem _mapManager = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly SectorServiceSystem _sectorService = default!;

    private readonly Queue<EntityUid> _drops = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeadDropComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<DeadDropComponent, GetVerbsEvent<InteractionVerb>>(AddSearchVerb);
        SubscribeLocalEvent<StationDeadDropComponent, ComponentStartup>(OnStationStartup);
        SubscribeLocalEvent<StationsGeneratedEvent>(OnStationsGenerated);
    }

    // There is some redundancy here - this should ideally run once over all the stations once worldgen is complete
    // Then once on any new stations if/when they're created.
    private void OnStationStartup(EntityUid stationUid, StationDeadDropComponent component, ComponentStartup _)
    {
    }

    public void CompromiseDeadDrop(EntityUid uid, DeadDropComponent component)
    {
        //Get our station.
        var station = _station.GetOwningStation(uid);
        //Remove the dead drop.
        RemComp<DeadDropComponent>(uid);
        //Find a new potential dead drop to spawn.
        var deadDropQuery = EntityManager.EntityQueryEnumerator<PotentialDeadDropComponent>();
        List<(EntityUid ent, PotentialDeadDropComponent comp)> potentialDeadDrops = new();
        while (deadDropQuery.MoveNext(out var ent, out var potentialDeadDrop))
        {
            // This potential dead drop is not on our station
            if (_station.GetOwningStation(ent) != station)
                continue;

            // This item already has an active dead drop, skip it
            if (HasComp<DeadDropComponent>(ent))
                continue;

            potentialDeadDrops.Add((ent, potentialDeadDrop));
        }

        // We have a potential dead drop, 
        if (potentialDeadDrops.Count > 0)
        {
            var item = _random.Pick(potentialDeadDrops);
            EnsureComp<DeadDropComponent>(item.ent);
        }
    }

    private void OnStationsGenerated(StationsGeneratedEvent args)
    {
        Log.Error("OnStationsGenerated!");
        if (!TryComp<SectorDeadDropComponent>(_sectorService.GetServiceEntity(), out var component))
        {
            Log.Error("OnStationsGenerated: no dead drop service!");
            return;
        }
        // Distribute total number of dead drops to assign between each station.
        var remainingDeadDrops = component.MaxSectorDeadDrops;

        Dictionary<EntityUid, (int assigned, int max)> assignedDeadDrops = new();
        var stationDropQuery = AllEntityQuery<StationDeadDropComponent>();
        while (stationDropQuery.MoveNext(out var station, out var stationDeadDrop))
        {
            var deadDropCount = int.Min(remainingDeadDrops, _random.Next(0, stationDeadDrop.MaxDeadDrops + 1));
            assignedDeadDrops[station] = (deadDropCount, stationDeadDrop.MaxDeadDrops);
            remainingDeadDrops -= deadDropCount;
            Log.Error($"OnStationsGenerated: assigned {deadDropCount} dead drops to station w/ UID {station.Id}");
        }

        // We have remaining dead drops, assign them to whichever stations have remaining space (in a random order)
        if (remainingDeadDrops > 0)
        {
            Log.Error($"OnStationsGenerated: {remainingDeadDrops} dead drops still to assign.");
            var stationList = assignedDeadDrops.Keys.ToList();
            _random.Shuffle(stationList);
            foreach (var station in stationList)
            {
                var dropTuple = assignedDeadDrops[station];

                // Insert as many dead drops here as we can.
                var remainingSpace = dropTuple.max - dropTuple.assigned;
                remainingSpace = int.Min(remainingSpace, remainingDeadDrops);
                dropTuple.assigned += remainingSpace;
                assignedDeadDrops[station] = dropTuple;

                // Adjust global counts.
                remainingDeadDrops -= remainingSpace;

                Log.Error($"OnStationsGenerated: assigned {remainingSpace} dead drops to station w/ UID {station.Id}.");

                if (remainingDeadDrops <= 0)
                    break;
            }
        }
        
        Log.Error($"OnStationsGenerated: enumerating PotentialDeadDropComponent w/EntityQueryEnumerator.");
        var pdq1 = EntityQueryEnumerator<PotentialDeadDropComponent>();
        while (pdq1.MoveNext(out var ent, out var potentialDrop))
        {
            Log.Error($"OnStationsGenerated: entity query enumerator: checking {ent}.");
        }

        // Log.Error($"OnStationsGenerated: enumerating PotentialDeadDropComponent w/EntityQuery.");
        // var pdq2 = new EntityQuery<PotentialDeadDropComponent>(true);
        // foreach (var ent in pdq2)
        // {
        //     Log.Error($"OnStationsGenerated: entity query enumerator: checking {ent.Owner}.");
        // }

        // For each station, distribute its assigned dead drops to potential dead drop components available on their grids.
        Dictionary<EntityUid, List<EntityUid>> potentialDropEntitiesPerStation = new();
        var potentialDropQuery = AllEntityQuery<PotentialDeadDropComponent>();
        Log.Error($"OnStationsGenerated: enumerating PotentialDeadDropComponent w/AllEntityQuery.");
        while (potentialDropQuery.MoveNext(out var ent, out var potentialDrop))
        {
            Log.Error($"OnStationsGenerated: checking entity's station {ent}.");
            var station = _station.GetOwningStation(ent);
            if (station is null)
            {
                Log.Error($"OnStationsGenerated: null station.");
                continue;
            }

            var stationUid = station.Value;
            if (assignedDeadDrops.ContainsKey(stationUid))
            {
                if (!potentialDropEntitiesPerStation.ContainsKey(stationUid))
                    potentialDropEntitiesPerStation[stationUid] = new List<EntityUid>();

                potentialDropEntitiesPerStation[stationUid].Add(ent);
                Log.Error($"OnStationsGenerated: associated potential dead drop: {stationUid}:{ent}.");
            }
            else
            {
                Log.Error($"OnStationsGenerated: assignedDeadDrops does not contain {stationUid}.");
            }
        }

        List<(EntityUid, EntityUid)> deadDropStationTuples = new();
        foreach (var (station, potentialDropList) in potentialDropEntitiesPerStation)
        {
            Log.Error($"OnStationsGenerated: assigning dead drops for station {station}.");
            if (!assignedDeadDrops.TryGetValue(station, out var stationDrops))
            {
                Log.Error($"OnStationsGenerated: station {station} not in map.");
                continue;
            }

            _random.Shuffle(potentialDropList);
            for (int i = 0; i < potentialDropList.Count && i < stationDrops.assigned; i++)
            {
                EnsureComp<DeadDropComponent>(potentialDropList[i]);
                deadDropStationTuples.Add((station, potentialDropList[i]));
                Log.Error($"OnStationsGenerated: assigned dead drop {station}:{potentialDropList[i]}.");
            }
        }

        // For each hint spawner, randomly generate a hint from the distributed dead drop locations, spawn the text for the note.
        var hintQuery = AllEntityQuery<DeadDropHintComponent>();
        while (hintQuery.MoveNext(out var ent, out var _))
        {
            var hintCount = _random.Next(2, 5);
            Log.Error($"OnStationsGenerated: assigning dead drop hints for {ent} (picking {hintCount}).");
            _random.Shuffle(deadDropStationTuples);

            var hintLines = new StringBuilder();
            var hints = 0;
            for (var i = 0; i < deadDropStationTuples.Count && hints < hintCount; i++)
            {
                var hintTuple = deadDropStationTuples[i];
                Log.Error($"OnStationsGenerated: assigning dead drop hint for {ent}: {hintTuple.Item2}.");
                string objectHintString;
                if (TryComp<PotentialDeadDropComponent>(hintTuple.Item2, out var potentialDeadDrop))
                    objectHintString = Loc.GetString(potentialDeadDrop.HintText);
                else
                    objectHintString = Loc.GetString("dead-drop-hint-generic");

                string stationHintString;
                if (TryComp<MetaDataComponent>(hintTuple.Item1, out var stationMetadata))
                    stationHintString = stationMetadata.EntityName;
                else
                    stationHintString = Loc.GetString("dead-drop-station-hint-generic");

                hintLines.AppendLine(Loc.GetString("dead-drop-hint-line", ("object", objectHintString), ("poi", stationHintString)));
                hints++;
            }
            var hintText = new StringBuilder();
            hintText.AppendLine(Loc.GetString("dead-drop-hint-note", ("drops", hintLines)));

            // Select some number of dead drops to hint
            if (TryComp<PaperComponent>(ent, out var paper))
                _paper.SetContent((ent, paper), hintText.ToString());

            // Hint generated, destroy component
            RemComp<DeadDropHintComponent>(ent);
        }
    }

    private void OnStartup(EntityUid paintingUid, DeadDropComponent component, ComponentStartup _)
    {
        //set up the timing of the first activation
        component.NextDrop = _timing.CurTime + TimeSpan.FromSeconds(_random.Next(component.MinimumCoolDown, component.MaximumCoolDown));
    }

    private void AddSearchVerb(EntityUid uid, DeadDropComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || args.Hands == null || _timing.CurTime < component.NextDrop)
            return;

        var xform = Transform(uid);
        var targetCoordinates = xform.Coordinates;

        // TODO: check global state if there's space to add our 
        //reset timer if there are 2 dead drop posters already active and this poster isn't one of them
        /*if (currentPosters >= component.MaxPosters && component.DeadDropActivated != true)
        {
            //reset the timer
            component.NextDrop = _timing.CurTime + TimeSpan.FromSeconds(_random.Next(component.MinimumCoolDown, component.MaximumCoolDown));
            return;
        }*/

        //here we build our dynamic verb. Using the object's sprite for now to make it more dynamic for the moment.
        InteractionVerb searchVerb = new()
        {
            IconEntity = GetNetEntity(uid),
            Act = () => SendDeadDrop(uid, component, args.User, args.Hands),
            Text = Loc.GetString("deaddrop-search-text"),
            Priority = 3
        };

        args.Verbs.Add(searchVerb);

        // if (component.DeadDropActivated == false)
        // {
        //     component.DeadDropActivated = true;
        // }
    }

    //spawning the dead drop.
    private void SendDeadDrop(EntityUid uid, DeadDropComponent component, EntityUid user, HandsComponent hands)
    {
        //simple check to make sure we dont allow multiple activations from a desynced verb window.
        if (_timing.CurTime < component.NextDrop)
            return;

        //relying entirely on shipyard capabilities, including using the shipyard map to spawn the items and ftl to bring em in
        if (_shipyard.ShipyardMap is not MapId shipyardMap)
            return;

        var options = new MapLoadOptions
        {
            LoadMap = false,
        };

        //load whatever grid was specified on the component, either a special dead drop or default
        if (!_map.TryLoad(shipyardMap, component.DropGrid, out var gridUids, options))
            return;

        //setup the radar properties
        _shuttle.SetIFFColor(gridUids[0], component.Color);
        _shuttle.AddIFFFlag(gridUids[0], IFFFlags.HideLabel);

        //this is where we set up all the information that FTL is going to need, including a new null entitiy as a destination target because FTL needs it for reasons?
        //dont ask me im just fulfilling FTL requirements.
        var dropLocation = _random.NextVector2(component.MinimumDistance, component.MaximumDistance);
        var mapId = Transform(user).MapID;

        //tries to get the map uid, if it fails, it will return which I would assume will make the component try again.
        if (!_mapManager.TryGetMap(mapId, out var mapUid))
        {
            return;
        }
        else
        {
            //this will spawn in the latest ship, and delete the oldest one available if the amount of ships exceeds 5.
            if (TryComp<ShuttleComponent>(gridUids[0], out var shuttle))
            {

                _shuttle.FTLToCoordinates(gridUids[0], shuttle, new EntityCoordinates(mapUid.Value, dropLocation), 0f, 0f, 35f);
                _drops.Enqueue(gridUids[0]);

                if (_drops.Count > 5)
                {
                    //removes the first element of the queue
                    var entityToRemove = _drops.Dequeue();
                    _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{entityToRemove} queued for deletion");
                    EntityManager.QueueDeleteEntity(entityToRemove);
                }
            }
        }

        //tattle on the smuggler here, but obfuscate it a bit if possible to just the grid it was summoned from.
        var channel = _prototypeManager.Index<RadioChannelPrototype>("Nfsd");
        var sender = Transform(user).GridUid ?? uid;

        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(user)} sent a dead drop to {dropLocation.ToString()} from {ToPrettyString(uid)} at {Transform(uid).Coordinates.ToString()}");

        // here we are just building a string for the hint paper so that it looks pretty and RP-like on the paper itself.
        var dropHint = new StringBuilder();
        dropHint.AppendLine(Loc.GetString("deaddrop-hint-pretext"));
        dropHint.AppendLine();
        dropHint.AppendLine(dropLocation.ToString());
        dropHint.AppendLine();
        dropHint.AppendLine(Loc.GetString("deaddrop-hint-posttext"));

        var paper = EntityManager.SpawnEntity(component.HintPaper, Transform(uid).Coordinates);

        if (TryComp(paper, out PaperComponent? paperComp))
        {
            _paper.SetContent((paper, paperComp), dropHint.ToString());
        }
        _meta.SetEntityName(paper, Loc.GetString("deaddrop-hint-name"));
        _meta.SetEntityDescription(paper, Loc.GetString("deaddrop-hint-desc"));
        _hands.PickupOrDrop(user, paper, handsComp: hands);

        //reset the timer
        component.NextDrop = _timing.CurTime + TimeSpan.FromSeconds(_random.Next(component.MinimumCoolDown, component.MaximumCoolDown));
        component.DeadDropCalled = true;
        //logic of posters ends here and logic of radio signals begins here

        //grabs NFSD Outpost to say the announcement
        var nfsdOutpost = new HashSet<Entity<MapGridComponent>>();
        _entityLookup.GetEntitiesOnMap(mapId, nfsdOutpost);

        //all possible locations which have dead drop posters
        string[] deadDropPossibleLocations = ["Tinnia's Rest", "Crazy Casey's Casino", "Grifty's Gas and Grub", "Expeditionary Lodge", "The Pit"];

        var pirateChannel = _prototypeManager.Index<RadioChannelPrototype>("Freelance");

        // TODO: make this time based (windowing?)
        int numDeadDropsReported = 0;
        if (TryComp<SectorDeadDropComponent>(_sectorService.GetServiceEntity(), out var sectorDeadDrop))
        {
            numDeadDropsReported = sectorDeadDrop.NumDeadDropsReported;
        }

        //checks if any of them are named NFSD Outpost
        foreach (var ent in nfsdOutpost)
        {
            if (MetaData(ent.Owner).EntityName.Equals("NFSD Outpost"))
            {
                //sends hints at the location depending on how many times a dead drop posters were activated in a POI
                if (numDeadDropsReported > 2)
                {
                    //tells the full location like it did earlier but only after the 3rd time a POI inovked a dead drop
                    _radio.SendRadioMessage(ent.Owner, Loc.GetString("deaddrop-correct-location", ("name", MetaData(sender).EntityName)), channel, uid);
                }
                else if (numDeadDropsReported == 2)
                {
                    //then sends a radio message telling a fake location and a real one
                    string[] locations = { MetaData(sender).EntityName, _random.Pick<string>(deadDropPossibleLocations) };
                    _random.Shuffle(locations);
                    _radio.SendRadioMessage(ent.Owner, Loc.GetString("deaddrop-fifty-fifty", ("location1", locations[0]), ("location2", locations[1])), channel, uid);
                }
                else if (numDeadDropsReported == 1)
                {
                    _radio.SendRadioMessage(ent.Owner, Loc.GetString("deaddrop-security-report"), channel, uid);
                }

                //tells the NFSD about the location of the dead drop after 15 minutes of it being active
                Timer.Spawn(TimeSpan.FromSeconds(component.RadioCoolDown), () =>
                {
                    _radio.SendRadioMessage(ent.Owner, Loc.GetString("deaddrop-nfsd", ("dropLocation", dropLocation)), channel, uid);
                });
            }

            //add a 1/3 chance for pirates to see the location of the smuggler after 15 minutes
            if (MetaData(ent.Owner).EntityName.Equals("Pirate's Cove") && _random.Next(0, 3) == 0)
            {
                Timer.Spawn(TimeSpan.FromSeconds(component.RadioCoolDown), () =>
                {
                    var sender = Transform(user).GridUid ?? uid;
                    _radio.SendRadioMessage(ent.Owner, Loc.GetString("deaddrop-pirate", ("ship", MetaData(sender).EntityName)), pirateChannel, uid);
                });
            }
        }
    }
}