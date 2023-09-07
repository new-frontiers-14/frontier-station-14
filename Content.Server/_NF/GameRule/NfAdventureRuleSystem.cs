using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Content.Server.Procedural;
using Content.Shared.Bank.Components;
using Content.Server.GameTicking.Events;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.Procedural;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Console;
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
    [Dependency] private readonly DungeonSystem _dunGen = default!;
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;

    private readonly HttpClient _httpClient = new();

    [ViewVariables]
    private List<(EntityUid, int)> _players = new();

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartingEvent>(OnStartup);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawningEvent);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndTextEvent);
    }

    private void OnRoundEndTextEvent(RoundEndTextAppendEvent ev)
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
        var depotMap = "/Maps/cargodepot.yml";
        var tinnia = "/Maps/tinnia.yml";
        var lpbravo = "/Maps/lpbravo.yml";
        var arena = "/Maps/arena.yml";
        var cove = "/Maps/cove.yml";
        var depotColor = new Color(55, 200, 55);
        var tinniaColor = new Color(55, 55, 200);
        var lpbravoColor = new Color(200, 55, 55);
        var mapId = GameTicker.DefaultMap;
        var depotOffset = _random.NextVector2(1500f, 2400f);

        if (_map.TryLoad(mapId, depotMap, out var depotUids, new MapLoadOptions
            {
                Offset = depotOffset
            }))
        {
            var meta = EnsureComp<MetaDataComponent>(depotUids[0]);
            meta.EntityName = "Cargo Depot A";
            _shuttle.SetIFFColor(depotUids[0], depotColor);
        }

        if (_map.TryLoad(mapId, tinnia, out var depotUid2s, new MapLoadOptions
            {
                Offset = _random.NextVector2(1275f, 1975f)
            }))
        {
            var meta = EnsureComp<MetaDataComponent>(depotUid2s[0]);
            meta.EntityName = "Tinnia's Rest";
            _shuttle.SetIFFColor(depotUid2s[0], tinniaColor);
        }

        depotOffset = _random.NextVector2(2600f, 3750f);
        if (_map.TryLoad(mapId, depotMap, out var depotUid3s, new MapLoadOptions
            {
                Offset = depotOffset
            }))
        {
            var meta = EnsureComp<MetaDataComponent>(depotUid3s[0]);
            meta.EntityName = "Cargo Depot B";
            _shuttle.SetIFFColor(depotUid3s[0], depotColor);
        }

        if (_map.TryLoad(mapId, lpbravo, out var depotUid4s, new MapLoadOptions
            {
                Offset = _random.NextVector2(1950f, 3500f)
            }))
        {
            var meta = EnsureComp<MetaDataComponent>(depotUid4s[0]);
            meta.EntityName = "Listening Point Bravo";
            _shuttle.SetIFFColor(depotUid4s[0], lpbravoColor);
            _shuttle.AddIFFFlag(depotUid4s[0], IFFFlags.HideLabel);
        }

        if (_map.TryLoad(mapId, arena, out var depotUid5s, new MapLoadOptions
            {
                Offset = _random.NextVector2(1500f, 3000f)
            }))
        {
            var meta = EnsureComp<MetaDataComponent>(depotUid5s[0]);
            meta.EntityName = "The Pit";
            _shuttle.SetIFFColor(depotUid5s[0], tinniaColor);
        }

        if (_map.TryLoad(mapId, cove, out var depotUid6s, new MapLoadOptions
            {
                Offset = _random.NextVector2(2250f, 4600f)
            }))
        {
            if (_prototypeManager.TryIndex<GameMapPrototype>("Cove", out var stationProto))
            {
                _station.InitializeNewStation(stationProto.Stations["Cove"], depotUid6s);
            }

            var meta = EnsureComp<MetaDataComponent>(depotUid6s[0]);
            meta.EntityName = "Pirate's Cove";
            _shuttle.SetIFFColor(depotUid6s[0], lpbravoColor);
            _shuttle.AddIFFFlag(depotUid6s[0], IFFFlags.HideLabel);
        }
        var dungenTypes = _prototypeManager.EnumeratePrototypes<DungeonConfigPrototype>();

        foreach (var dunGen in dungenTypes)
        {
            var seed = _random.Next();
            var offset = _random.NextVector2(2750f, 4400f);
            if (!_map.TryLoad(mapId, "/Maps/spaceplatform.yml", out var grids, new MapLoadOptions
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
            //because they are all offset
            _dunGen.GenerateDungeon(dunGen, grids[0], mapGrid, (Vector2i) offset, seed);
        }
        foreach (var dunGen in dungenTypes)
        {

            var seed = _random.Next();
            var offset = _random.NextVector2(3800f, 8500f);
            if (!_map.TryLoad(mapId, "/Maps/spaceplatform.yml", out var grids, new MapLoadOptions
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
