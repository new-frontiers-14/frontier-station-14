using System.Reflection;
using System.Text;
using Content.Server._NF.Smuggling.Components;
using Content.Server.Administration.Logs;
using Content.Server.Radio.EntitySystems;
using Content.Server.Shipyard.Systems;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
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

    private readonly List<EntityUid> _poi = [];
    private readonly Queue<EntityUid> _drops = [];
    private int currentPosters = 0;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeadDropComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<DeadDropComponent, GetVerbsEvent<InteractionVerb>>(AddSearchVerb);
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

        //reset timer if there are 2 dead drop posters already active and this poster isn't one of them
        if (currentPosters >= component.MaxPosters && component.DeadDropActivated != true)
        {
            //reset the timer
            component.NextDrop = _timing.CurTime + TimeSpan.FromSeconds(_random.Next(component.MinimumCoolDown, component.MaximumCoolDown));
            return;
        }

        //here we build our dynamic verb. Using the object's sprite for now to make it more dynamic for the moment.
        InteractionVerb searchVerb = new()
        {
            IconEntity = GetNetEntity(uid),
            Act = () => SendDeadDrop(uid, component, args.User, args.Hands),
            Text = Loc.GetString("deaddrop-search-text"),
            Priority = 3
        };

        args.Verbs.Add(searchVerb);

        if (component.DeadDropActivated == false) 
        {
            component.DeadDropActivated = true;
            currentPosters++;
        }
    }

    //toggles the scanned boolean from ForensicScannerSystem.cs
    public static void ToggleScanned(DeadDropComponent component)
    {
        component.PosterScanned = true;
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
        component.DeadDropActivated = false;
        currentPosters--;

        //logic of posters ends here and logic of radio signals begins here

        component.DeadDropCalled = true;

        //grabs NFSD Outpost to say the announcement
        var nfsdOutpost = new HashSet<Entity<MapGridComponent>>();
        _entityLookup.GetEntitiesOnMap(mapId, nfsdOutpost);

        //all possible locations which have dead drop posters
        string[] deadDropPossibleLocations = ["Tinnia's Rest", "Crazy Casey's Casino", "Grifty's Gas and Grub", "Expeditionary Lodge", "The Pit"];

        var pirateChannel = _prototypeManager.Index<RadioChannelPrototype>("Freelance");

        //checks if any of them are named NFSD Outpost
        foreach (var ent in nfsdOutpost)
        {
            if (MetaData(ent.Owner).EntityName.Equals("NFSD Outpost"))
            {
                //adds the poi to a list to count for the hints
                _poi.Add(sender);
                var count = 0;

                foreach (var poi in _poi)
                {
                    if (poi == sender)
                    {
                        count++;
                    }
                }

                //sends hints at the location depending on how many times a dead drop posters were activated in a POI
                if (count == 1)
                {
                    _radio.SendRadioMessage(ent.Owner, Loc.GetString("deaddrop-security-report"), channel, uid);
                }
                else if (count == 2)
                {
                    var location1 = "";
                    var location2 = "";

                    //rolls a 50/50 chance to see whether the real location would be on the left or right
                    if (_random.Next(0, 2) == 0)
                    {
                        location1 = MetaData(sender).EntityName;
                        location2 = deadDropPossibleLocations[_random.Next(0, deadDropPossibleLocations.Length)];
                        while (location1.Equals(location2))
                        {
                            location2 = deadDropPossibleLocations[_random.Next(0, deadDropPossibleLocations.Length)];
                        }
                    }
                    else
                    {
                        location2 = MetaData(sender).EntityName;
                        location1 = deadDropPossibleLocations[_random.Next(0, deadDropPossibleLocations.Length)];
                        while (location2.Equals(location1))
                        {
                            location1 = deadDropPossibleLocations[_random.Next(0, deadDropPossibleLocations.Length)];
                        }
                    }

                    //then sends a radio message telling a fake location and a real one
                    _radio.SendRadioMessage(ent.Owner, Loc.GetString("deaddrop-fifty-fifty", ("location1", location1), ("location2", location2)), channel, uid);
                }
                else if (count > 2)
                {
                    //tells the full location like it did earlier but only after the 3rd time a POI inovked a dead drop
                    _radio.SendRadioMessage(ent.Owner, Loc.GetString("deaddrop-correct-location", ("name", MetaData(sender).EntityName)), channel, uid);
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