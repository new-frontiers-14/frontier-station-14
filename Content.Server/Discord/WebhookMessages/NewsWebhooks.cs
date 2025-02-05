using Content.Server.Discord;
using Content.Shared.MassMedia.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using System.Threading.Tasks;
using Content.Shared.CCVar;
using Content.Server.CartridgeLoader.Cartridges;
using System.Text.RegularExpressions;

namespace Content.Server.Discord.WebhookMessages;

public sealed class NewsWebhooks : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly DiscordWebhook _discord = default!;
    [Dependency] private readonly ILogManager _log = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = _log.GetSawmill("discord-news");

        _sawmill.Info("[NewsWebhooks] Initialized and ready to receive articles.");
    }

    /// <summary>
    /// Handles a published news article and sends it to Discord.
    /// This is called directly from NewsSystem.cs.
    /// </summary>
    public void HandleNewsPublished(NewsArticle article)
    {
        _sawmill.Info($"[NewsWebhooks] Directly received article: {article.Title} by {article.Author}");

        SendNewsToDiscord(article);
    }

    private bool TryParseWebhookUrl(string url, out string id, out string token)
    {
        id = string.Empty;
        token = string.Empty;

        // Discord webhook format: https://discord.com/api/webhooks/{id}/{token}
        var parts = url.Split('/');
        if (parts.Length < 7) // Ensure the URL has enough segments
            return false;

        id = parts[5];   // The 6th element is the webhook ID
        token = parts[6]; // The 7th element is the webhook token
        return true;
    }

    private async Task SendNewsToDiscord(NewsArticle article)
    {
        // Fetch the webhook URL from config
        var webhookUrl = _cfg.GetCVar(CCVars.DiscordNewsWebhook);
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            _sawmill.Warning("Discord webhook URL is not configured. Skipping news posting.");
            return;
        }

        // Extract ID and Token
        if (!TryParseWebhookUrl(webhookUrl, out var webhookId, out var webhookToken))
        {
            _sawmill.Error($"Invalid Discord webhook URL format: {webhookUrl}");
            return;
        }

        _sawmill.Info($"[NewsWebhooks] Attempting to send news to Discord: {article.Title}");

        // Format content using SS14ToDiscordFormatter
        string formattedContent = SS14ToDiscordFormatter.ConvertToDiscordMarkup(article.Content);

        // Construct payload
        var payload = new WebhookPayload
        {
            Username = "NewsBot",
            Embeds = new List<WebhookEmbed>
            {
                new()
                {
                    Title = article.Title,
                    Description = formattedContent,
                    Color = 3447003, // Discord blue
                    Footer = new WebhookEmbedFooter
                    {
                        Text = $"Written by: {article.Author ?? "Unknown"}"
                    }
                }
            }
        };

        // Send webhook with the correct identifier
        var response = await _discord.CreateMessage(new WebhookIdentifier(webhookId, webhookToken), payload);

        if (response.IsSuccessStatusCode)
        {
            _sawmill.Info($"[NewsWebhooks] Successfully sent news to Discord: {article.Title}");
        }
        else
        {
            var errorMessage = await response.Content.ReadAsStringAsync();
            _sawmill.Error($"[NewsWebhooks] Failed to send news to Discord. Status Code: {response.StatusCode}, Response: {errorMessage}");
        }
    }
}

/// <summary>
/// Converts SS14 news markup to Discord-compatible markup.
/// </summary>
public static class SS14ToDiscordFormatter
{
    public static string ConvertToDiscordMarkup(string body)
    {
        if (string.IsNullOrEmpty(body))
            return string.Empty;

        // Convert headings (maps [head=1-3] to bold, since Discord has no true headings)
        body = Regex.Replace(body, @"\[head=\d\](.*?)\[/head\]", "**$1**");

        // Convert color tags (Discord does not support colored text, so remove them)
        body = Regex.Replace(body, @"\[color=[^\]]+\](.*?)\[/color\]", "$1");

        // Convert italic, bold, and bullet point tags
        body = Regex.Replace(body, @"\[italic\](.*?)\[/italic\]", "*$1*");
        body = Regex.Replace(body, @"\[bold\](.*?)\[/bold\]", "**$1**");
        body = Regex.Replace(body, @"\[bullet\](.*?)\[/bullet\]", "- $1");

        return body;
    }
}
