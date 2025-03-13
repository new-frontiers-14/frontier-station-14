using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Content.Server._NF.Bank;
using Content.Server._NF.GameRule.Components;
using Content.Server._NF.GameTicking.Events;
using Content.Server.Cargo.Components;
using Content.Server.Discord;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Presets;
using Content.Server.GameTicking.Rules;
using Content.Shared._NF.Bank;
using Content.Shared._NF.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Robust.Server;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._NF.GameRule;

/// <summary>
/// This handles the dungeon and trading post spawning, as well as round end capitalism summary
/// </summary>
public sealed class NFAdventureRuleSystem : GameRuleSystem<NFAdventureRuleComponent>
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly BankSystem _bank = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly PointOfInterestSystem _poi = default!;
    [Dependency] private readonly IBaseServer _baseServer = default!;
    [Dependency] private readonly IEntitySystemManager _entSys = default!;

    private readonly HttpClient _httpClient = new();

    private readonly ProtoId<GamePresetPrototype> _fallbackPresetID = "NFPirates";

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

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawningEvent);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetachedEvent);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        _player.PlayerStatusChanged += PlayerManagerOnPlayerStatusChanged;
    }

    protected override void AppendRoundEndText(EntityUid uid, NFAdventureRuleComponent component, GameRuleComponent gameRule, ref RoundEndTextAppendEvent ev)
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
        ReportLedger();
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

    protected override void Started(EntityUid uid, NFAdventureRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        var mapUid = GameTicker.DefaultMap;

        //First, we need to grab the list and sort it into its respective spawning logics
        List<PointOfInterestPrototype> depotProtos = new();
        List<PointOfInterestPrototype> marketProtos = new();
        List<PointOfInterestPrototype> requiredProtos = new();
        List<PointOfInterestPrototype> optionalProtos = new();
        Dictionary<string, List<PointOfInterestPrototype>> remainingUniqueProtosBySpawnGroup = new();

        var currentPreset = _ticker.CurrentPreset?.ID ?? _fallbackPresetID;

        foreach (var location in _proto.EnumeratePrototypes<PointOfInterestPrototype>())
        {
            // Check if any preset is accepted (empty) or if current preset is supported.
            if (location.SpawnGamePreset.Length > 0 && !location.SpawnGamePreset.Contains(currentPreset))
                continue;

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
        _poi.GenerateDepots(mapUid, depotProtos, out component.CargoDepots);
        _poi.GenerateMarkets(mapUid, marketProtos, out component.MarketStations);
        _poi.GenerateRequireds(mapUid, requiredProtos, out component.RequiredPois);
        _poi.GenerateOptionals(mapUid, optionalProtos, out component.OptionalPois);
        _poi.GenerateUniques(mapUid, remainingUniqueProtosBySpawnGroup, out component.UniquePois);

        base.Started(uid, component, gameRule, args);

        // Using invalid entity, we don't have a relevant entity to reference here.
        RaiseLocalEvent(EntityUid.Invalid, new StationsGeneratedEvent(), broadcast: true); // TODO: attach this to a meaningful entity.
    }

    private async Task ReportRound(string message, int color = 0x77DDE7)
    {
        Logger.InfoS("discord", message);
        string webhookUrl = _cfg.GetCVar(NFCCVars.DiscordLeaderboardWebhook);
        if (webhookUrl == string.Empty)
            return;

        var serverName = _baseServer.ServerName;
        var gameTicker = _entSys.GetEntitySystemOrNull<GameTicker>();
        var runId = gameTicker != null ? gameTicker.RoundId : 0;

        var payload = new WebhookPayload
        {
            Embeds = new List<Embed>
            {
                new()
                {
                    Title = Loc.GetString("adventure-webhook-list-start"),
                    Description = message,
                    Color = color,
                    Footer = new EmbedFooter
                    {
                        Text = Loc.GetString(
                            "adventure-webhook-footer",
                            ("serverName", serverName),
                            ("roundId", runId)),
                    },
                },
            },
        };
        await SendWebhookPayload(webhookUrl, payload);
    }

    private async Task ReportLedger(int color = 0xBF863F)
    {
        string webhookUrl = _cfg.GetCVar(NFCCVars.DiscordLeaderboardWebhook);
        if (webhookUrl == string.Empty)
            return;

        var ledgerPrintout = _bank.GetLedgerPrintout();
        if (string.IsNullOrEmpty(ledgerPrintout))
            return;
        Logger.InfoS("discord", ledgerPrintout);

        var serverName = _baseServer.ServerName;
        var gameTicker = _entSys.GetEntitySystemOrNull<GameTicker>();
        var runId = gameTicker != null ? gameTicker.RoundId : 0;

        var payload = new WebhookPayload
        {
            Embeds = new List<Embed>
            {
                new()
                {
                    Title = Loc.GetString("adventure-webhook-ledger-start"),
                    Description = ledgerPrintout,
                    Color = color,
                    Footer = new EmbedFooter
                    {
                        Text = Loc.GetString(
                            "adventure-webhook-footer",
                            ("serverName", serverName),
                            ("roundId", runId)),
                    },
                },
            },
        };
        await SendWebhookPayload(webhookUrl, payload);
    }

    private async Task SendWebhookPayload(string webhookUrl, WebhookPayload payload)
    {
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
