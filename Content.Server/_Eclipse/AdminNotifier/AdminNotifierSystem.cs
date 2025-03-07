using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Popups;
using Content.Shared.Interaction;
using Content.Shared._Eclipse.CCVar;
using Content.Server.Discord;
using Robust.Shared.Configuration;
using Content.Server.GameTicking;
using Robust.Server;
using Content.Shared.Mind;

namespace Content.Server._Eclipse.AdminNotifier;

public sealed class AdminNotifierSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly DiscordWebhook _discord = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IBaseServer _baseServer = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;

    private string _webhookUrl = default!;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(EclipseCCVars.DiscordInteractAlertWebhook, SetWebhookUrl, true);

        SubscribeLocalEvent<AdminNotifierComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<AdminNotifierComponent, InteractHandEvent>(OnInteractHand);
    }

    private void SetWebhookUrl(string webhookUrl)
    {
        _webhookUrl = webhookUrl;
    }

    private void OnInteractUsing(EntityUid uid, AdminNotifierComponent component, InteractUsingEvent args)
    {
        SendDiscordMessage(args.User, args.Target, component.AlertMessage);
    }

    private void OnInteractHand(EntityUid uid, AdminNotifierComponent component, InteractHandEvent args)
    {
        SendDiscordMessage(args.User, args.Target, component.AlertMessage);
    }

    private async void SendDiscordMessage(EntityUid uid, EntityUid targetUid, LocId alertMessage)
    {
        if (string.IsNullOrWhiteSpace(_webhookUrl))
            return;

        try
        {
            var webhookData = await _discord.GetWebhook(_webhookUrl);

            var webhookIdentifier = webhookData.Value.ToIdentifier();

            var meta = MetaData(uid);

            var targetMeta = MetaData(targetUid);

            var username = string.Empty;
            if (_mindSystem.TryGetMind(uid, out _, out var mind) && mind.Session != null)
            {
                username = mind.Session.Name;
            }

            var message = Loc.GetString(
                alertMessage,
                ("entityName", meta.EntityName),
                ("username", username),
                ("uid", uid.ToString()),
                ("targetName", targetMeta.EntityName)
                );

            var payload = new WebhookPayload
            {
                Embeds = new List<WebhookEmbed>
                {
                    new()
                    {
                        Title = Loc.GetString("admin-notifier-alert-title"),
                        Description = message,
                        Color = 0xFF0000, // red
                        Footer = new WebhookEmbedFooter
                        {
                            Text = Loc.GetString(
                                "admin-notifier-alert-footer",
                                ("serverName", _baseServer.ServerName),
                                ("roundId", _gameTicker.RoundId)),
                        },
                    },
                },
            };

            await _discord.CreateMessage(webhookIdentifier, payload);
        }
        catch (Exception e)
        {
            Log.Error($"Error while sending discord interaction alert message:\n{e}");
        }
    }
}
