using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Content._NF.Shared.GameRule;
using Content.Server.Procedural;
using Content.Shared.Bank.Components;
using Content.Server.GameTicking.Events;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.Procedural;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Console;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Map.Components;
using Content.Shared.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Cargo.Components;
using Content.Server.Maps;
using Content.Server.Station.Systems;
using Content.Shared.CCVar;
using Content.Shared.NF14.CCVar;
using Robust.Shared.Configuration;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// This handles the dungeon and trading post spawning, as well as round end capitalism summary
/// </summary>
public sealed class NfAdventureRuleSystem : GameRuleSystem<AdventureRuleComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly DungeonSystem _dunGen = default!;
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;

    private readonly HttpClient _httpClient = new();

    [ViewVariables]
    private List<(EntityUid, int)> _players = new();

    private float _distanceOffset = 1f;
    private List<EntityUid> _depotStations = new();
    private List<EntityUid> _marketStations = new();
    private List<EntityUid> _optionalStations = new();
    private List<EntityUid> _requiredStations = new();
    private List<EntityUid> _uniqueStations = new();

    private MapId _mapId;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartingEvent>(OnStartup);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawningEvent);
    }

    protected override void AppendRoundEndText(EntityUid uid, AdventureRuleComponent component, GameRuleComponent gameRule, ref RoundEndTextAppendEvent ev)
    {
        var profitText = Loc.GetString($"adventure-mode-profit-text");
        var lossText = Loc.GetString($"adventure-mode-loss-text");
        ev.AddLine(Loc.GetString("adventure-list-start"));
        var allScore = new List<Tuple<string, int>>();

        foreach (var player in _players)
        {
            if (!TryComp<BankAccountComponent>(player.Item1, out var bank) || !TryComp<MetaDataComponent>(player.Item1, out var meta))
                continue;

            var profit = bank.Balance - player.Item2;
            ev.AddLine($"- {meta.EntityName} {profitText} {profit} Spesos");
            allScore.Add(new Tuple<string, int>(meta.EntityName, profit));
        }

        if (!(allScore.Count >= 1))
            return;

        var relayText = Loc.GetString("adventure-list-high");
        relayText += '\n';
        var highScore = allScore.OrderByDescending(h => h.Item2).ToList();

        for (var i = 0; i < 10 && i < highScore.Count; i++)
        {
            relayText += $"{highScore.First().Item1} {profitText} {highScore.First().Item2.ToString()} Spesos";
            relayText += '\n';
            highScore.Remove(highScore.First());
        }
        relayText += Loc.GetString("adventure-list-low");
        relayText += '\n';
        highScore.Reverse();
        for (var i = 0; i < 10 && i < highScore.Count; i++)
        {
            relayText += $"{highScore.First().Item1} {lossText} {highScore.First().Item2.ToString()} Spesos";
            relayText += '\n';
            highScore.Remove(highScore.First());
        }
        ReportRound(relayText);
    }

    private void OnPlayerSpawningEvent(PlayerSpawnCompleteEvent ev)
    {
        if (ev.Player.AttachedEntity is { Valid: true } mobUid)
        {
            _players.Add((mobUid, ev.Profile.BankBalance));
            EnsureComp<CargoSellBlacklistComponent>(mobUid);
        }
    }

    private void OnStartup(RoundStartingEvent ev)
    {
        _mapId = GameTicker.DefaultMap;
        _depotStations.Clear();
        _marketStations.Clear();
        _optionalStations.Clear();
        _requiredStations.Clear();
        _uniqueStations.Clear();

        _distanceOffset = _configurationManager.GetCVar(NF14CVars.POIDistanceModifier);

        //First, we need to grab the list and sort it into its respective spawning logics
        var allLocationList = _prototypeManager.EnumeratePrototypes<PointOfInterestPrototype>().ToList();
        Logger.Info("all locations contains " + allLocationList.Count.ToString());
        var depotList = allLocationList.Where(w => w.SpawnGroup == "CargoDepot").ToList();
        foreach (var proto in depotList)
        {
            allLocationList.Remove(proto);
        }
        GenerateDepots(depotList);

        var marketList = allLocationList.Where(w => w.SpawnGroup == "MarketStation").ToList();
        foreach (var proto in marketList)
        {
            allLocationList.Remove(proto);
        }
        GenerateMarkets(marketList);

        var requiredList = allLocationList.Where(w => w.AlwaysSpawn == true).ToList();
        foreach (var proto in requiredList)
        {
            allLocationList.Remove(proto);
        }
        GenerateRequireds(requiredList);

        var optionalList = allLocationList.Where(w => w.SpawnGroup == "Optional").ToList();
        foreach (var proto in optionalList)
        {
            allLocationList.Remove(proto);
        }
        GenerateOptionals(optionalList);

        // the remainder are done on a per-poi-per-group basis
        var uniqueList = allLocationList;

        GenerateUniques(uniqueList);


        var mapId = GameTicker.DefaultMap;
        var dungenTypes = _prototypeManager.EnumeratePrototypes<DungeonConfigPrototype>();

        foreach (var dunGen in dungenTypes)
        {

            var seed = _random.Next();
            var offset = _random.NextVector2(3000f, 8500f) * _distanceOffset;
            if (!_map.TryLoad(mapId, "/Maps/_NF/Dungeon/spaceplatform.yml", out var grids, new MapLoadOptions
                {
                    Offset = offset
                }))
            {
                continue;
            }

            var mapGrid = EnsureComp<MapGridComponent>(grids[0]);
            _shuttle.AddIFFFlag(grids[0], IFFFlags.HideLabel);
            _console.WriteLine(null, $"dungeon spawned at {offset}");
            offset = new Vector2i(0, 0);

            //pls fit the grid I beg, this is so hacky
            //its better now but i think i need to do a normalization pass on the dungeon configs
            //because they are all offset. confirmed good size grid, just need to fix all the offsets.
            _dunGen.GenerateDungeon(dunGen, grids[0], mapGrid, (Vector2i) offset, seed);
        }
    }

    private void GenerateDepots(IEnumerable<PointOfInterestPrototype> depotPrototypes)
    {
        //For depots, we want them to fill a circular type dystance formula to try to keep them as far apart as possible
        //Therefore, we will be taking our range properties and treating them as magnitudes of a direction vector divided
        //by the number of depots set in our corresponding cvar
        var protoList = depotPrototypes.ToList();
        var depotCount = _configurationManager.GetCVar(NF14CVars.CargoDepots);
        var rotation = 2 * Math.PI / depotCount;
        var rotationOffset = _random.NextAngle() / depotCount;

        for (int i = 0; i < depotCount && protoList.Count > 0; i++)
        {
            var proto = _random.Pick(protoList);
            Vector2i offset = new Vector2i(_random.Next(proto.RangeMin, proto.RangeMax), 0);
            offset.Rotate(rotationOffset);
            rotationOffset += rotation;
            if (TrySpawnPoiGrid(proto, offset, out var depotUid) && depotUid is {Valid: true} depot)
            {
                _depotStations.Add(depot);
            }
        }
    }

    private void GenerateMarkets(IEnumerable<PointOfInterestPrototype> marketPrototypes)
    {
        //For market stations, we are going to allow for a bit of randomness and a different offset configuration. We dont
        //want copies of this one, since these can be more themed and duplicate names, for instance, can make for a less
        //ideal world

        var protoList = marketPrototypes.ToList();
        var marketCount = _configurationManager.GetCVar(NF14CVars.MarketStations);
        for (int i = 0; i < marketCount && protoList.Count > 0; i++)
        {
            var proto = _random.PickAndTake(protoList);
            var offset = _random.NextVector2(proto.RangeMin, proto.RangeMax);

            if (TrySpawnPoiGrid(proto, offset, out var marketUid) && marketUid is {Valid: true} market)
            {
                _marketStations.Add(market);
            }
        }
    }

    private void GenerateOptionals(IEnumerable<PointOfInterestPrototype> optionalPrototypes)
    {
        //Stations that do not have a defined grouping in their prototype get a default of "Optional" and get put into the
        //generic random rotation of POIs. This should include traditional places like Tinnia's rest, the Science Lab, The Pit,
        //and most RP places. This will essentially put them all into a pool to pull from, and still does not use the RNG function.

        var protoList = optionalPrototypes.ToList();
        var optionalCount = _configurationManager.GetCVar(NF14CVars.OptionalStations);
        for (int i = 0; i < optionalCount && protoList.Count > 0; i++)
        {

            var proto = _random.PickAndTake(protoList);
            var offset = _random.NextVector2(proto.RangeMin, proto.RangeMax);

            if (TrySpawnPoiGrid(proto, offset, out var optionalUid) && optionalUid is {Valid: true} uid)
            {
                _optionalStations.Add(uid);
            }
        }
    }

    private void GenerateRequireds(IEnumerable<PointOfInterestPrototype> requiredPrototypes)
    {
        //Stations are required are ones that are vital to function but otherwise still follow a generic random spawn logic
        //Traditionally these would be stations like Expedition Lodge, NFSD station, Prison/Courthouse POI, etc.
        //There are no limit to these, and any prototype marked alwaysSpawn = true will get pulled out of any list that isnt Markets/Depots
        //And will always appear every time, and also will not be included in other optional/dynamic lists

        var protoList = requiredPrototypes.ToList();

        foreach (var proto in protoList)
        {
            var offset = _random.NextVector2(proto.RangeMin, proto.RangeMax);

            if (TrySpawnPoiGrid(proto, offset, out var requiredUid) && requiredUid is {Valid: true} uid)
            {
                _requiredStations.Add(uid);
            }
        }
    }

    private void GenerateUniques(IEnumerable<PointOfInterestPrototype> uniquePrototypes)
    {
        //Unique locations are semi-dynamic groupings of POIs that rely each independantly on the SpawnChance per POI prototype
        //Since these are the remainder, and logically must have custom-designated groupings, we can then know to subdivide
        //our random pool into these found groups.
        //To do this with an equal distribution on a per-POI, per-round percentage basis, we are going to ensure a random
        //pick order of which we analyze our weighted chances to spawn, and if successful, remove every entry of that group
        //entirely.

        var protoList = uniquePrototypes.ToList();

        while (protoList.Count >= 1)
        {
            var proto = _random.PickAndTake(protoList);

            var chance = _random.NextFloat(0, 1);
            if (chance <= proto.SpawnChance)
            {
                var offset = _random.NextVector2(proto.RangeMin, proto.RangeMax);

                if (TrySpawnPoiGrid(proto, offset, out var optionalUid) && optionalUid is {Valid: true} uid)
                {
                    _uniqueStations.Add(uid);

                    var groupList = protoList.Where(w => w.SpawnGroup == proto.SpawnGroup).ToList();

                    foreach (var groupItem in groupList)
                    {
                        protoList.Remove(groupItem);
                    }
                }
            }
        }
    }

    private bool TrySpawnPoiGrid(PointOfInterestPrototype proto, Vector2 offset, out EntityUid? gridUid)
    {
        gridUid = null;
        if (_map.TryLoad(_mapId, proto.GridPath.ToString(), out var mapUids, new MapLoadOptions
            {
                Offset = offset * _distanceOffset
            }))
        {
            if (_prototypeManager.TryIndex<GameMapPrototype>(proto.ID, out var stationProto))
            {
                _station.InitializeNewStation(stationProto.Stations[proto.ID], mapUids, proto.Name);
            }

            foreach (var grid in mapUids)
            {
                var meta = EnsureComp<MetaDataComponent>(grid);
                _meta.SetEntityName(grid, proto.Name, meta);
                _shuttle.SetIFFColor(grid, proto.IffColor);
                if (proto.IsHidden)
                {
                    _shuttle.AddIFFFlag(grid, IFFFlags.HideLabel);
                }
            }
            gridUid = mapUids[0];
            return true;
        }

        return false;
    }

    private async Task ReportRound(String message,  int color = 0x77DDE7)
    {
        Logger.InfoS("discord", message);
        String _webhookUrl = _configurationManager.GetCVar(CCVars.DiscordLeaderboardWebhook);
        if (_webhookUrl == string.Empty)
            return;

        var payload = new WebhookPayload
        {
            Embeds = new List<Embed>
            {
                new()
                {
                    Title = Loc.GetString("adventure-list-start"),
                    Description = message,
                    Color = color,
                },
            },
        };

        var ser_payload = JsonSerializer.Serialize(payload);
        var content = new StringContent(ser_payload, Encoding.UTF8, "application/json");
        var request = await _httpClient.PostAsync($"{_webhookUrl}?wait=true", content);
        var reply = await request.Content.ReadAsStringAsync();
        if (!request.IsSuccessStatusCode)
        {
            Logger.ErrorS("mining", $"Discord returned bad status code when posting message: {request.StatusCode}\nResponse: {reply}");
        }
    }

// https://discord.com/developers/docs/resources/channel#message-object-message-structure
    private struct WebhookPayload
    {
        [JsonPropertyName("username")] public string? Username { get; set; } = null;

        [JsonPropertyName("avatar_url")] public string? AvatarUrl { get; set; } = null;

        [JsonPropertyName("content")] public string Message { get; set; } = "";

        [JsonPropertyName("embeds")] public List<Embed>? Embeds { get; set; } = null;

        [JsonPropertyName("allowed_mentions")]
        public Dictionary<string, string[]> AllowedMentions { get; set; } =
            new()
            {
                { "parse", Array.Empty<string>() },
            };

        public WebhookPayload()
        {
        }
    }

// https://discord.com/developers/docs/resources/channel#embed-object-embed-structure
    private struct Embed
    {
        [JsonPropertyName("title")] public string Title { get; set; } = "";

        [JsonPropertyName("description")] public string Description { get; set; } = "";

        [JsonPropertyName("color")] public int Color { get; set; } = 0;

        [JsonPropertyName("footer")] public EmbedFooter? Footer { get; set; } = null;

        public Embed()
        {
        }
    }

// https://discord.com/developers/docs/resources/channel#embed-object-embed-footer-structure
    private struct EmbedFooter
    {
        [JsonPropertyName("text")] public string Text { get; set; } = "";

        [JsonPropertyName("icon_url")] public string? IconUrl { get; set; }

        public EmbedFooter()
        {
        }
    }
}
