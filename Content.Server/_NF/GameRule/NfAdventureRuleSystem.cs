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
        ev.AddLine(Loc.GetString("adventure-list-start"));
        foreach (var player in _players)
        {
            if (!TryComp<BankAccountComponent>(player.Item1, out var bank) || !TryComp<MetaDataComponent>(player.Item1, out var meta))
                continue;

            var profit = bank.Balance - player.Item2;
            ev.AddLine($"- {meta.EntityName} {profitText} {profit} Spesos");
        }

        ReportRound(ev.Text);
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
        var depotColor = new Color(55, 200, 55);
        var tinniaColor = new Color(55, 55, 200);
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

        ;
        if (_map.TryLoad(mapId, tinnia, out var depotUid2s, new MapLoadOptions
            {
                Offset = -depotOffset
            }))
        {
            var meta = EnsureComp<MetaDataComponent>(depotUid2s[0]);
            meta.EntityName = "Tinnia's Rest";
            _shuttle.SetIFFColor(depotUid2s[0], tinniaColor);
        }

        ;
        depotOffset = _random.NextVector2(2300f, 3400f);
        if (_map.TryLoad(mapId, depotMap, out var depotUid3s, new MapLoadOptions
            {
                Offset = depotOffset
            }))
        {
            var meta = EnsureComp<MetaDataComponent>(depotUid3s[0]);
            meta.EntityName = "Cargo Depot B";
            _shuttle.SetIFFColor(depotUid3s[0], depotColor);
        }

        ;
        if (_map.TryLoad(mapId, depotMap, out var depotUid4s, new MapLoadOptions
            {
                Offset = -depotOffset
            }))
        {
            var meta = EnsureComp<MetaDataComponent>(depotUid4s[0]);
            meta.EntityName = "Cargo Depot C";
            _shuttle.SetIFFColor(depotUid4s[0], depotColor);
        }

        ;
        var dungenTypes = _prototypeManager.EnumeratePrototypes<DungeonConfigPrototype>();

        foreach (var dunGen in dungenTypes)
        {

            var seed = _random.Next();
            var offset = _random.NextVector2(2100f, 4500f);
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
            offset = new Vector2(0, 0);

            //pls fit the grid I beg, this is so hacky
            //its better now but i think i need to do a normalization pass on the dungeon configs
            //because they are all offset
            _dunGen.GenerateDungeon(dunGen, grids[0], mapGrid, (Vector2i) offset, seed);
        }
        foreach (var dunGen in dungenTypes)
        {

            var seed = _random.Next();
            var offset = _random.NextVector2(2300f, 6500f);
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
            offset = new Vector2(0, 0);

            //pls fit the grid I beg, this is so hacky
            //its better now but i think i need to do a normalization pass on the dungeon configs
            //because they are all offset. confirmed good size grid, just need to fix all the offsets.
            _dunGen.GenerateDungeon(dunGen, grids[0], mapGrid, (Vector2i) offset, seed);
        }
    }

    private async Task ReportRound(String message)
    {
        Logger.InfoS("discord", message);
        String _webhookUrl = _configurationManager.GetCVar(CCVars.DiscordEndRoundWebhook);
        if (_webhookUrl == string.Empty)
            return;

        var payload = new WebhookPayload{ Content = message };
        var ser_payload = JsonSerializer.Serialize(payload);
        var content = new StringContent(ser_payload, Encoding.UTF8, "application/json");
        var request = await _httpClient.PostAsync($"{_webhookUrl}?wait=true", content);
        var reply = await request.Content.ReadAsStringAsync();
        if (!request.IsSuccessStatusCode)
        {
            Logger.ErrorS("mining", $"Discord returned bad status code when posting message: {request.StatusCode}\nResponse: {reply}");
        }
    }

    private struct WebhookPayload
    {
        [JsonPropertyName("content")]
        public String Content { get; set; }
    }
}
