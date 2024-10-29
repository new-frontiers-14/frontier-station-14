using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Content.Shared.CCVar;
using Content.Shared._NF.CCVar; // Frontier
using Content.Server.Maps;
using Content.Shared.GameTicking;
using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Server.Nyanotrasen.RoundNotifications;

/// <summary>
/// Listen game events and send notifications to Discord
/// </summary>
public sealed class RoundNotificationsSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IGameMapManager _gameMapManager = default!;

    private ISawmill _sawmill = default!;
    private readonly HttpClient _httpClient = new();

    private string _webhookUrl = String.Empty;
    private string _roleId = String.Empty;
    private bool _roundStartOnly;
    private string _serverName = string.Empty;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<RoundStartedEvent>(OnRoundStarted);
        SubscribeLocalEvent<RoundEndedEvent>(OnRoundEnded);

        _config.OnValueChanged(NFCCVars.DiscordRoundWebhook, value => _webhookUrl = value, true); // Frontier: namespaced CVar
        _config.OnValueChanged(NFCCVars.DiscordRoundRoleId, value => _roleId = value, true); // Frontier: namespaced CVar
        _config.OnValueChanged(NFCCVars.DiscordRoundStartOnly, value => _roundStartOnly = value, true); // Frontier: namespaced CVar
        _config.OnValueChanged(CVars.GameHostName, OnServerNameChanged, true);

        _sawmill = IoCManager.Resolve<ILogManager>().GetSawmill("notifications");
    }

    private void OnServerNameChanged(string obj)
    {
        _serverName = obj;
    }

    private void OnRoundRestart(RoundRestartCleanupEvent e)
    {
        if (String.IsNullOrEmpty(_webhookUrl))
            return;

        var text = Loc.GetString("discord-round-new");

        SendDiscordMessage(text, true, 0x91B2C7);
    }

    private void OnRoundStarted(RoundStartedEvent e)
    {
        if (String.IsNullOrEmpty(_webhookUrl))
            return;

        var map = _gameMapManager.GetSelectedMap();
        var mapName = map?.MapName ?? Loc.GetString("discord-round-unknown-map");
        var text = Loc.GetString("discord-round-start",
            ("id", e.RoundId),
            ("map", mapName));

        SendDiscordMessage(text, false);
    }

    private void OnRoundEnded(RoundEndedEvent e)
    {
        if (String.IsNullOrEmpty(_webhookUrl) || _roundStartOnly)
            return;

        var text = Loc.GetString("discord-round-end",
            ("id", e.RoundId),
            ("hours", Math.Truncate(e.RoundDuration.TotalHours)),
            ("minutes", e.RoundDuration.Minutes),
            ("seconds", e.RoundDuration.Seconds));

        SendDiscordMessage(text, false, 0xB22B27);
    }

    private async void SendDiscordMessage(string text, bool ping = false, int color = 0x41F097)
    {

        // Limit server name to 1500 characters, in case someone tries to be a little funny
        var serverName = _serverName[..Math.Min(_serverName.Length, 1500)];
        var message = "";
        if (!String.IsNullOrEmpty(_roleId) && ping)
            message = $"<@&{_roleId}>";

        // Build the embed
        var payload = new WebhookPayload
        {
            Message = message,
            Embeds = new List<Embed>
            {
                new()
                {
                    Title = Loc.GetString("discord-round-title"),
                    Description = text,
                    Color = color,
                    Footer = new EmbedFooter
                    {
                        Text = $"{serverName}"
                    },
                },
            },
        };
        if (!String.IsNullOrEmpty(_roleId) && ping)
            payload.AllowedMentions = new Dictionary<string, string[]> {{ "roles", new []{ _roleId } }};

        var request = await _httpClient.PostAsync($"{_webhookUrl}?wait=true",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

        var content = await request.Content.ReadAsStringAsync();
        if (!request.IsSuccessStatusCode)
        {
            _sawmill.Log(LogLevel.Error,
                $"Discord returned bad status code when posting message: {request.StatusCode}\nResponse: {content}");
            return;
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
