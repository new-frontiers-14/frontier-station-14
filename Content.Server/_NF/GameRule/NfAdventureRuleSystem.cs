using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Content.Shared._NF.GameRule;
using Content.Server.Procedural;
using Content.Server._NF.GameTicking.Events;
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
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Maps;
using Content.Server.Station.Systems;
using Content.Shared.CCVar;
using Content.Shared._NF.CCVar; // Frontier
using Robust.Shared.Configuration;
using Robust.Shared.Physics.Components;
using Content.Server.Shuttles.Components;
using Content.Shared._NF.Bank;
using Content.Shared.Tiles;
using Content.Server._NF.PublicTransit.Components;
using Content.Server._NF.GameRule.Components;
using Content.Server.Bank;
using Robust.Shared.Player;
using Robust.Shared.Network;
using Content.Shared.GameTicking;
using Robust.Shared.Enums;
using Robust.Server.Player;

namespace Content.Server._NF.GameRule;

/// <summary>
/// This handles the dungeon and trading post spawning, as well as round end capitalism summary
/// </summary>
public sealed class NfAdventureRuleSystem : GameRuleSystem<AdventureRuleComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly DungeonSystem _dunGen = default!;
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly BankSystem _bank = default!;

    private readonly HttpClient _httpClient = new();

    public sealed class PlayerRoundBankInformation
    {
        // Initial balance, obtained on spawn
        public int StartBalance;
        // Ending balance, obtained on game end or detach (NOTE: multiple detaches possible), whichever happens first.
        public int EndBalance;
        // Entity name: used for display purposes ("The Feel of Fresh Bills earned 100,000 spesos")
        public string Name;
        // User ID: used to validate incoming information.
        // If, for whatever reason, another player takes over this character, their initial balance is inaccurate.
        public NetUserId UserId;

        public PlayerRoundBankInformation(int startBalance, string name, NetUserId userId)
        {
            StartBalance = startBalance;
            EndBalance = -1;
            Name = name;
            UserId = userId;
        }
    }

    // A list of player bank account information stored by the controlled character's entity.
    [ViewVariables]
    private Dictionary<EntityUid, PlayerRoundBankInformation> _players = new();

    private float _distanceOffset = 1f;
    private List<Vector2> _stationCoords = new();

    private MapId _mapId;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawningEvent);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetachedEvent);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        _playerManager.PlayerStatusChanged += PlayerManagerOnPlayerStatusChanged;
    }

    protected override void AppendRoundEndText(EntityUid uid, AdventureRuleComponent component, GameRuleComponent gameRule, ref RoundEndTextAppendEvent ev)
    {
        ev.AddLine(Loc.GetString("adventure-list-start"));
        var allScore = new List<Tuple<string, int>>();

        foreach (var (player, playerInfo) in _players)
        {
            var endBalance = playerInfo.EndBalance;
            if (_bank.TryGetBalance(player, out var bankBalance))
            {
                endBalance = bankBalance;
            }

            // Check if endBalance is valid (non-negative)
            if (endBalance < 0)
                continue;

            var profit = endBalance - playerInfo.StartBalance;
            string summaryText;
            if (profit < 0)
            {
                summaryText = Loc.GetString("adventure-list-loss", ("amount", BankSystemExtensions.ToSpesoString(-profit)));
            }
            else
            {
                summaryText = Loc.GetString("adventure-list-profit", ("amount", BankSystemExtensions.ToSpesoString(profit)));
            }
            ev.AddLine($"- {playerInfo.Name} {summaryText}");
            allScore.Add(new Tuple<string, int>(playerInfo.Name, profit));
        }

        if (!(allScore.Count >= 1))
            return;

        var relayText = Loc.GetString("adventure-webhook-list-high");
        relayText += '\n';
        var highScore = allScore.OrderByDescending(h => h.Item2).ToList();

        for (var i = 0; i < 10 && highScore.Count > 0; i++)
        {
            if (highScore.First().Item2 < 0)
                break;
            var profitText = Loc.GetString("adventure-webhook-top-profit", ("amount", BankSystemExtensions.ToSpesoString(highScore.First().Item2)));
            relayText += $"{highScore.First().Item1} {profitText}";
            relayText += '\n';
            highScore.RemoveAt(0);
        }
        relayText += '\n'; // Extra line separating the highest and lowest scores
        relayText += Loc.GetString("adventure-webhook-list-low");
        relayText += '\n';
        highScore.Reverse();
        for (var i = 0; i < 10 && highScore.Count > 0; i++)
        {
            if (highScore.First().Item2 > 0)
                break;
            var lossText = Loc.GetString("adventure-webhook-top-loss", ("amount", BankSystemExtensions.ToSpesoString(-highScore.First().Item2)));
            relayText += $"{highScore.First().Item1} {lossText}";
            relayText += '\n';
            highScore.RemoveAt(0);
        }
        ReportRound(relayText);
    }

    private void OnPlayerSpawningEvent(PlayerSpawnCompleteEvent ev)
    {
        if (ev.Player.AttachedEntity is { Valid: true } mobUid)
        {
            EnsureComp<CargoSellBlacklistComponent>(mobUid);

            // Store player info with the bank balance - we have it directly, and BankSystem won't have a cache yet.
            if (!_players.ContainsKey(mobUid))
                _players[mobUid] = new PlayerRoundBankInformation(ev.Profile.BankBalance, MetaData(mobUid).EntityName, ev.Player.UserId);
        }
    }

    private void OnPlayerDetachedEvent(PlayerDetachedEvent ev)
    {
        if (ev.Entity is not { Valid: true } mobUid)
            return;

        if (_players.ContainsKey(mobUid))
        {
            if (_players[mobUid].UserId == ev.Player.UserId &&
                _bank.TryGetBalance(ev.Player, out var bankBalance))
            {
                _players[mobUid].EndBalance = bankBalance;
            }
        }
    }

    private void PlayerManagerOnPlayerStatusChanged(object? _, SessionStatusEventArgs e)
    {
        // Treat all disconnections as being possibly final.
        if (e.NewStatus != SessionStatus.Disconnected ||
            e.Session.AttachedEntity == null)
            return;

        var mobUid = e.Session.AttachedEntity.Value;
        if (_players.ContainsKey(mobUid))
        {
            if (_players[mobUid].UserId == e.Session.UserId &&
                _bank.TryGetBalance(e.Session, out var bankBalance))
            {
                _players[mobUid].EndBalance = bankBalance;
            }
        }
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _players.Clear();
    }

    protected override void Started(EntityUid uid, AdventureRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        _mapId = GameTicker.DefaultMap;

        _distanceOffset = _configurationManager.GetCVar(NFCCVars.POIDistanceModifier);
        _stationCoords = new List<Vector2>();

        //First, we need to grab the list and sort it into its respective spawning logics
        List<PointOfInterestPrototype> depotProtos = new();
        List<PointOfInterestPrototype> marketProtos = new();
        List<PointOfInterestPrototype> requiredProtos = new();
        List<PointOfInterestPrototype> optionalProtos = new();
        Dictionary<string, List<PointOfInterestPrototype>> remainingUniqueProtosBySpawnGroup = new();

        foreach (var location in _prototypeManager.EnumeratePrototypes<PointOfInterestPrototype>())
        {
            if (location.SpawnGroup == "CargoDepot")
                depotProtos.Add(location);
            else if (location.SpawnGroup == "MarketStation")
                marketProtos.Add(location);
            else if (location.SpawnGroup == "Required")
                requiredProtos.Add(location);
            else if (location.SpawnGroup == "Optional")
                optionalProtos.Add(location);
            else // the remainder are done on a per-poi-per-group basis
            {
                if (!remainingUniqueProtosBySpawnGroup.ContainsKey(location.SpawnGroup))
                    remainingUniqueProtosBySpawnGroup[location.SpawnGroup] = new();
                remainingUniqueProtosBySpawnGroup[location.SpawnGroup].Add(location);
            }
        }
        GenerateDepots(depotProtos, out component.CargoDepots);
        GenerateMarkets(marketProtos, out component.MarketStations);
        GenerateRequireds(requiredProtos, out component.RequiredPois);
        GenerateOptionals(optionalProtos, out component.OptionalPois);
        GenerateUniques(remainingUniqueProtosBySpawnGroup, out component.UniquePois);

        base.Started(uid, component, gameRule, args);

        // Using invalid entity, we don't have a relevant entity to reference here.
        RaiseLocalEvent(EntityUid.Invalid, new StationsGeneratedEvent(), broadcast: true); // TODO: attach this to a meaningful entity.

        foreach (var dungeonProto in component.SpaceDungeons)
        {
            if (!_prototypeManager.TryIndex<DungeonConfigPrototype>(dungeonProto, out var dunGen))
                continue;

            var seed = _random.Next();
            var offset = GetRandomPOICoord(3000f, 8500f, true);
            if (!_map.TryLoad(_mapId, "/Maps/_NF/Dungeon/spaceplatform.yml", out var grids,
                    new MapLoadOptions
                    {
                        Offset = offset
                    }))
            {
                continue;
            }

            var mapGrid = EnsureComp<MapGridComponent>(grids[0]);
            _shuttle.AddIFFFlag(grids[0], IFFFlags.HideLabel);
            _console.WriteLine(null, $"dungeon spawned at {offset}");

            string dungeonName = Loc.GetString("adventure-space-dungeon-name", ("dungeonPrototype", dungeonProto));
            _meta.SetEntityName(grids[0], dungeonName);

            //pls fit the grid I beg, this is so hacky
            //its better now but i think i need to do a normalization pass on the dungeon configs
            //because they are all offset. confirmed good size grid, just need to fix all the offsets.
            _dunGen.GenerateDungeon(dunGen, dunGen.ID, grids[0], mapGrid, new Vector2i(0, 0), seed);
            AddStationCoordsToSet(offset);
        }
    }

    private void GenerateDepots(List<PointOfInterestPrototype> depotPrototypes, out List<EntityUid> depotStations)
    {
        //For depots, we want them to fill a circular type dystance formula to try to keep them as far apart as possible
        //Therefore, we will be taking our range properties and treating them as magnitudes of a direction vector divided
        //by the number of depots set in our corresponding cvar

        depotStations = new List<EntityUid>();
        var depotCount = _configurationManager.GetCVar(NFCCVars.CargoDepots);
        var rotation = 2 * Math.PI / depotCount;
        var rotationOffset = _random.NextAngle() / depotCount;

        for (int i = 0; i < depotCount && depotPrototypes.Count > 0; i++)
        {
            var proto = _random.Pick(depotPrototypes);
            Vector2i offset = new Vector2i((int) (_random.Next(proto.RangeMin, proto.RangeMax) * _distanceOffset), 0);
            offset = offset.Rotate(rotationOffset);
            rotationOffset += rotation;
            // Append letter to depot name.

            string overrideName = proto.Name;
            if (i < 26)
                overrideName += $" {(char) ('A' + i)}"; // " A" ... " Z"
            else
                overrideName += $" {i + 1}"; // " 27", " 28"...
            if (TrySpawnPoiGrid(proto, offset, out var depotUid, overrideName: overrideName) && depotUid is { Valid: true } depot)
            {
                depotStations.Add(depot);
                AddStationCoordsToSet(offset); // adjust list of actual station coords
            }
        }
    }

    private void GenerateMarkets(List<PointOfInterestPrototype> marketPrototypes, out List<EntityUid> marketStations)
    {
        //For market stations, we are going to allow for a bit of randomness and a different offset configuration. We dont
        //want copies of this one, since these can be more themed and duplicate names, for instance, can make for a less
        //ideal world

        marketStations = new List<EntityUid>();
        var marketCount = _configurationManager.GetCVar(NFCCVars.MarketStations);
        _random.Shuffle(marketPrototypes);
        int marketsAdded = 0;
        foreach (var proto in marketPrototypes)
        {
            if (marketsAdded >= marketCount)
                break;

            var offset = GetRandomPOICoord(proto.RangeMin, proto.RangeMax, true);

            if (TrySpawnPoiGrid(proto, offset, out var marketUid) && marketUid is { Valid: true } market)
            {
                marketStations.Add(market);
                marketsAdded++;
                AddStationCoordsToSet(offset);
            }
        }
    }

    private void GenerateOptionals(List<PointOfInterestPrototype> optionalPrototypes, out List<EntityUid> optionalStations)
    {
        //Stations that do not have a defined grouping in their prototype get a default of "Optional" and get put into the
        //generic random rotation of POIs. This should include traditional places like Tinnia's rest, the Science Lab, The Pit,
        //and most RP places. This will essentially put them all into a pool to pull from, and still does not use the RNG function.

        optionalStations = new List<EntityUid>();
        var optionalCount = _configurationManager.GetCVar(NFCCVars.OptionalStations);
        _random.Shuffle(optionalPrototypes);
        int optionalsAdded = 0;
        foreach (var proto in optionalPrototypes)
        {
            if (optionalsAdded >= optionalCount)
                break;

            var offset = GetRandomPOICoord(proto.RangeMin, proto.RangeMax, true);

            if (TrySpawnPoiGrid(proto, offset, out var optionalUid) && optionalUid is { Valid: true } uid)
            {
                optionalStations.Add(uid);
                AddStationCoordsToSet(offset);
            }
        }
    }

    private void GenerateRequireds(List<PointOfInterestPrototype> requiredPrototypes, out List<EntityUid> requiredStations)
    {
        //Stations are required are ones that are vital to function but otherwise still follow a generic random spawn logic
        //Traditionally these would be stations like Expedition Lodge, NFSD station, Prison/Courthouse POI, etc.
        //There are no limit to these, and any prototype marked alwaysSpawn = true will get pulled out of any list that isnt Markets/Depots
        //And will always appear every time, and also will not be included in other optional/dynamic lists

        requiredStations = new List<EntityUid>();
        foreach (var proto in requiredPrototypes)
        {
            var offset = GetRandomPOICoord(proto.RangeMin, proto.RangeMax, true);

            if (TrySpawnPoiGrid(proto, offset, out var requiredUid) && requiredUid is { Valid: true } uid)
            {
                requiredStations.Add(uid);
                AddStationCoordsToSet(offset);
            }
        }
    }

    private void GenerateUniques(Dictionary<string, List<PointOfInterestPrototype>> uniquePrototypes, out List<EntityUid> uniqueStations)
    {
        //Unique locations are semi-dynamic groupings of POIs that rely each independantly on the SpawnChance per POI prototype
        //Since these are the remainder, and logically must have custom-designated groupings, we can then know to subdivide
        //our random pool into these found groups.
        //To do this with an equal distribution on a per-POI, per-round percentage basis, we are going to ensure a random
        //pick order of which we analyze our weighted chances to spawn, and if successful, remove every entry of that group
        //entirely.

        uniqueStations = new List<EntityUid>();
        foreach (var prototypeList in uniquePrototypes.Values)
        {
            // Try to spawn 
            _random.Shuffle(prototypeList);
            foreach (var proto in prototypeList)
            {
                var chance = _random.NextFloat(0, 1);
                if (chance <= proto.SpawnChance)
                {
                    var offset = GetRandomPOICoord(proto.RangeMin, proto.RangeMax, true);

                    if (TrySpawnPoiGrid(proto, offset, out var optionalUid) && optionalUid is { Valid: true } uid)
                    {
                        uniqueStations.Add(uid);
                        AddStationCoordsToSet(offset);
                        break;
                    }
                }
            }
        }
    }

    private bool TrySpawnPoiGrid(PointOfInterestPrototype proto, Vector2 offset, out EntityUid? gridUid, string? overrideName = null)
    {
        gridUid = null;
        if (_map.TryLoad(_mapId, proto.GridPath.ToString(), out var mapUids,
                new MapLoadOptions
                {
                    Offset = offset,
                    Rotation = _random.NextAngle()
                }))
        {

            string stationName = string.IsNullOrEmpty(overrideName) ? proto.Name : overrideName;

            if (_prototypeManager.TryIndex<GameMapPrototype>(proto.ID, out var stationProto))
            {
                _station.InitializeNewStation(stationProto.Stations[proto.ID], mapUids, stationName);
            }

            // Cache our damping strength
            float dampingStrength = proto.CanMove ? 0.05f : 999999f;

            foreach (var grid in mapUids)
            {
                var meta = EnsureComp<MetaDataComponent>(grid);
                _meta.SetEntityName(grid, stationName, meta);

                EnsureComp<IFFComponent>(grid);
                _shuttle.SetIFFColor(grid, proto.IFFColor);
                _shuttle.AddIFFFlag(grid, proto.Flags);

                if (!proto.AllowIFFChanges)
                {
                    _shuttle.SetIFFReadOnly(grid, true);
                }

                // Ensure damping for each grid in the POI - set the shuttle component if it exists just to be safe
                var physics = EnsureComp<PhysicsComponent>(grid);
                _physics.SetAngularDamping(grid, physics, dampingStrength);
                _physics.SetLinearDamping(grid, physics, dampingStrength);
                if (TryComp<ShuttleComponent>(grid, out var shuttle))
                {
                    shuttle.AngularDamping = dampingStrength;
                    shuttle.LinearDamping = dampingStrength;
                }

                if (proto.BusStop)
                {
                    EnsureComp<StationTransitComponent>(grid);
                }

                if (proto.GridProtection != GridProtectionFlags.None)
                {
                    var prot = EnsureComp<ProtectedGridComponent>(grid);
                    if (proto.GridProtection.HasFlag(GridProtectionFlags.FloorRemoval))
                        prot.PreventFloorRemoval = true;
                    if (proto.GridProtection.HasFlag(GridProtectionFlags.FloorPlacement))
                        prot.PreventFloorPlacement = true;
                    if (proto.GridProtection.HasFlag(GridProtectionFlags.RcdUse))
                        prot.PreventRCDUse = true;
                    if (proto.GridProtection.HasFlag(GridProtectionFlags.EmpEvents))
                        prot.PreventEmpEvents = true;
                    if (proto.GridProtection.HasFlag(GridProtectionFlags.Explosions))
                        prot.PreventExplosions = true;
                    if (proto.GridProtection.HasFlag(GridProtectionFlags.ArtifactTriggers))
                        prot.PreventArtifactTriggers = true;
                }
            }
            gridUid = mapUids[0];
            return true;
        }

        return false;
    }

    private Vector2 GetRandomPOICoord(float unscaledMinRange, float unscaledMaxRange, bool scaleRange)
    {
        int numRetries = int.Max(_configurationManager.GetCVar(NFCCVars.POIPlacementRetries), 0);
        float minDistance = float.Max(_configurationManager.GetCVar(NFCCVars.MinPOIDistance), 0); // Constant at the end to avoid NaN weirdness

        Vector2 coords = _random.NextVector2(unscaledMinRange, unscaledMaxRange);
        if (scaleRange)
            coords *= _distanceOffset;
        for (int i = 0; i < numRetries; i++)
        {
            bool positionIsValid = true;
            foreach (var station in _stationCoords)
            {
                if (Vector2.Distance(station, coords) < minDistance)
                {
                    positionIsValid = false;
                    break;
                }
            }

            // We have a valid position
            if (positionIsValid)
                break;

            // No vector yet, get next value.
            coords = _random.NextVector2(unscaledMinRange, unscaledMaxRange);
            if (scaleRange)
                coords *= _distanceOffset;
        }

        return coords;
    }

    private void AddStationCoordsToSet(Vector2 coords)
    {
        _stationCoords.Add(coords);
    }

    private async Task ReportRound(string message, int color = 0x77DDE7)
    {
        Logger.InfoS("discord", message);
        String webhookUrl = _configurationManager.GetCVar(CCVars.DiscordLeaderboardWebhook);
        if (webhookUrl == string.Empty)
            return;

        var payload = new WebhookPayload
        {
            Embeds = new List<Embed>
            {
                new()
                {
                    Title = Loc.GetString("adventure-webhook-list-start"),
                    Description = message,
                    Color = color,
                },
            },
        };

        var ser_payload = JsonSerializer.Serialize(payload);
        var content = new StringContent(ser_payload, Encoding.UTF8, "application/json");
        var request = await _httpClient.PostAsync($"{webhookUrl}?wait=true", content);
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
