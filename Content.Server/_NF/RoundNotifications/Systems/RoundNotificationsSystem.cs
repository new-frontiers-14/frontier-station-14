using Content.Server.Discord;
using Content.Shared._NF.CCVar;
using Content.Server.Maps;
using Content.Shared.GameTicking;
using Robust.Shared;
using Robust.Shared.Configuration;
using Content.Server._NF.RoundNotifications.Events;

namespace Content.Server._NF.RoundNotifications.Systems;

/// <summary>
/// Listen for game events and send notifications to Discord.
/// </summary>
/// <remarks>
/// Updated version of the old Nyanotrasen RoundNotificationsSystem
/// </remarks>
public sealed class RoundNotificationsSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IGameMapManager _gameMapManager = default!;
    [Dependency] private readonly DiscordWebhook _discord = default!;

    private ISawmill _sawmill = default!;

    private string _roleId = string.Empty;
    private bool _roundStartOnly;
    private string _serverName = string.Empty;
    private WebhookIdentifier? _webhookIdentifier;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<RoundStartedEvent>(OnRoundStarted);
        SubscribeLocalEvent<RoundEndMessageEvent>(OnRoundEnded);

        Subs.CVar(_config, CVars.GameHostName, value => _serverName = value, true);
        Subs.CVar(_config, NFCCVars.DiscordRoundRoleId, value => _roleId = value, true);
        Subs.CVar(_config, NFCCVars.DiscordRoundStartOnly, value => _roundStartOnly = value, true);
        Subs.CVar(_config, NFCCVars.DiscordRoundWebhook, value =>
        {
            if (!string.IsNullOrWhiteSpace(value))
                _discord.GetWebhook(value, data => _webhookIdentifier = data.ToIdentifier());
            else
                _webhookIdentifier = null;
        }, true);

        _sawmill = Logger.GetSawmill("notifications");
    }

    private void OnRoundRestart(RoundRestartCleanupEvent e)
    {
        if (_webhookIdentifier == null)
            return;

        var text = Loc.GetString("discord-round-new");

        SendDiscordMessage(text, true, 0x91B2C7);
    }

    private void OnRoundStarted(RoundStartedEvent e)
    {
        if (_webhookIdentifier == null)
            return;

        var map = _gameMapManager.GetSelectedMap();
        var mapName = map?.MapName ?? Loc.GetString("discord-round-unknown-map");
        var text = Loc.GetString("discord-round-start",
            ("id", e.RoundId),
            ("map", mapName));

        SendDiscordMessage(text, false);
    }

    private void OnRoundEnded(RoundEndMessageEvent e)
    {
        if (_webhookIdentifier == null || _roundStartOnly)
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
        if (_webhookIdentifier == null)
            return;

        try
        {
            // Limit server name to 1500 characters, in case someone tries to be a little funny
            var serverName = _serverName[..Math.Min(_serverName.Length, 1500)];
            var message = "";
            if (!string.IsNullOrEmpty(_roleId) && ping)
                message = $"<@&{_roleId}>";

            // Build the embed
            var payload = new WebhookPayload
            {
                Content = message,
                Embeds = new List<WebhookEmbed>
                {
                    new()
                    {
                        Title = Loc.GetString("discord-round-title"),
                        Description = text,
                        Color = color,
                        Footer = new WebhookEmbedFooter
                        {
                            Text = $"{serverName}"
                        },
                    },
                },
            };
            if (!string.IsNullOrEmpty(_roleId) && ping)
            {
                var mentions = new WebhookMentions();
                mentions.Roles.Add(_roleId);
                payload.AllowedMentions = mentions;
            }

            var request = await _discord.CreateMessage(_webhookIdentifier.Value, payload);
            if (!request.IsSuccessStatusCode)
            {
                var content = await request.Content.ReadAsStringAsync();
                _sawmill.Error($"Discord returned bad status code when posting message: {request.StatusCode}\nResponse: {content}");
                return;
            }
        }
        catch (Exception e)
        {
            _sawmill.Error($"Error while sending discord round status message:\n{e}");
        }
    }
}
