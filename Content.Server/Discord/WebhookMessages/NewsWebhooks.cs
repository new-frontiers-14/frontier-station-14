using System;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Collections.Generic;
using Content.Server.Discord;
using Content.Shared.MassMedia.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using System.Threading.Tasks;
using Content.Shared.CCVar;

namespace Content.Server.Discord.WebhookMessages;

public sealed class NewsWebhooks : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly DiscordWebhook _discord = default!;
    [Dependency] private readonly ILogManager _log = default!;

    private ISawmill _sawmill = default!;
    private readonly Dictionary<string, ulong> _messageIds = new(); // Store message IDs
    private readonly List<NewsArticle> _roundEndNewsBuffer = new(); // Store News Buffer for End of Round Posting
    private bool _liveNewsPosting = false;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = _log.GetSawmill("discord-news");
        _sawmill.Info("[NewsWebhooks] Initialized and ready to receive articles.");
        _liveNewsPosting = _cfg.GetCVar(CCVars.DiscordLiveNewsPosting); // Override if different.
    }

    /// <summary>
    /// Handles a published news article and sends it to Discord.
    /// This is called directly from NewsSystem.cs.
    /// </summary>
    public async Task SendNewsToDiscord(NewsArticle article, bool roundEnd = false)
    {
        if (!_liveNewsPosting && !roundEnd)
        {
            _roundEndNewsBuffer.Add(article);
            _sawmill.Info($"{article.Title} added to _roundEndNewsBuffer");
            return;
        }

        var webhookUrl = _cfg.GetCVar(CCVars.DiscordNewsWebhook);
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            _sawmill.Warning("Discord webhook URL is not configured. Skipping news posting.");
            return;
        }

        if (!TryParseWebhookUrl(webhookUrl, out var webhookId, out var webhookToken))
        {
            _sawmill.Error($"Invalid Discord webhook URL format: {webhookUrl}");
            return;
        }

        _sawmill.Info($"Attempting to send news to Discord: {article.Title}");

        var payload = new WebhookPayload
        {
            Username = "NewsBot",
            Embeds = new List<WebhookEmbed>
            {
                new()
                {
                    Title = article.Title,
                    Description = SS14ToDiscordFormatter.ConvertToDiscordMarkup(article.Content),
                    Color = 3447003,
                    Footer = new WebhookEmbedFooter { Text = $"Written by: {article.Author ?? "Unknown"}" }
                }
            }
        };

        var response = await _discord.CreateMessage(new WebhookIdentifier(webhookId, webhookToken), payload);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("id", out var idElement))
            {
                string? messageIdString = idElement.GetString(); // Extract ID as a nullable string

                if (string.IsNullOrEmpty(messageIdString))
                {
                    _sawmill.Error("Failed to retrieve message ID from Discord response.");
                    return;
                }

                if (ulong.TryParse(messageIdString, out ulong messageId))
                {
                    _messageIds[article.Title] = messageId;
                    _sawmill.Info($"Successfully stored message ID: {messageId} for article: {article.Title}");
                }
                else
                {
                    _sawmill.Error($"Failed to parse message ID: {messageIdString} from Discord response.");
                }
            }
            else
            {
                _sawmill.Error("Discord response did not contain a message ID. Cannot store for deletion.");
            }
        }
        else
        {
            var errorMessage = await response.Content.ReadAsStringAsync();
            _sawmill.Error($"Failed to send news to Discord. Status Code: {response.StatusCode}, Response: {errorMessage}");
        }
    }

    /// <summary>
    /// Deletes a news post from Discord using its stored message ID.
    /// </summary>
    public async Task DeleteNewsFromDiscord(NewsArticle article)
    {
        if (!_liveNewsPosting)
        {
            _roundEndNewsBuffer.Remove(article);
            _sawmill.Info($"{article.Title} removed from _roundEndNewsBuffer");
            return;
        }

        string articleTitle = article.Title;
        _sawmill.Info($"Attempting to delete news post: {articleTitle}");

        if (!_messageIds.TryGetValue(articleTitle, out var messageId))
        {
            _sawmill.Warning($"No stored message ID found for article: {articleTitle}. Deletion skipped.");
            return;
        }

        var webhookUrl = _cfg.GetCVar(CCVars.DiscordNewsWebhook);
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            _sawmill.Warning("Discord webhook URL is not configured. Skipping deletion.");
            return;
        }

        if (!TryParseWebhookUrl(webhookUrl, out var webhookId, out var webhookToken))
        {
            _sawmill.Error($"Invalid Discord webhook URL format: {webhookUrl}");
            return;
        }

        _sawmill.Info($"Sending delete request to Discord for article '{articleTitle}' with Message ID: {messageId}");

        var response = await _discord.DeleteMessage(new WebhookIdentifier(webhookId, webhookToken), messageId);

        if (response.IsSuccessStatusCode)
        {
            _sawmill.Info($"Successfully deleted Discord message for article: {articleTitle}");
            _messageIds.Remove(articleTitle); // Remove from storage after deletion
        }
        else
        {
            var errorMessage = await response.Content.ReadAsStringAsync();
            _sawmill.Error($"Failed to delete Discord message. Status Code: {response.StatusCode}, Response: {errorMessage}");
        }
    }

    /// <summary>
    /// Parses a Discord webhook URL into its ID and token.
    /// </summary>
    private bool TryParseWebhookUrl(string url, out string id, out string token)
    {
        id = string.Empty;
        token = string.Empty;

        var parts = url.Split('/');
        if (parts.Length < 7) // Ensure the URL has enough segments
            return false;

        id = parts[5];   // The 6th element is the webhook ID
        token = parts[6]; // The 7th element is the webhook token
        return true;
    }

    public async Task OnRoundEndHandleNews()
    {
        if (_liveNewsPosting)
        {
            _sawmill.Info("[NewsWebhooks] Live posting enabled. Clearing buffer without posting.");
            _roundEndNewsBuffer.Clear(); // Clear the buffer pending new round
            return;
        }

        if (_roundEndNewsBuffer.Count == 0)
        {
            _sawmill.Info("[NewsWebhooks] No stored articles to post at round end.");
            return;
        }

        _sawmill.Info($"[NewsWebhooks] Posting {_roundEndNewsBuffer.Count} articles to Discord at round end.");

        // Copy the list to avoid modification exceptions
        var articlesToSend = _roundEndNewsBuffer.ToArray();

        foreach (var article in articlesToSend)
        {
            await SendNewsToDiscord(article, true);
        }

        // Clear the buffer after all articles are sent
        _roundEndNewsBuffer.Clear();
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

        // Convert headings ([head=1], [head=2], etc.)
        body = Regex.Replace(body, @"\[head=(\d+)\](.*?)\[/head\]", match =>
        {
            int level = int.Parse(match.Groups[1].Value);
            string text = match.Groups[2].Value;

            return level switch
            {
                1 => $"# {text}", // Largest
                2 => $"## {text}", // Medium
                3 => $"### {text}", // Smallest
                _ => text // Fallback (remove if unknown)
            };
        });

        // Remove unsupported color tags
        body = Regex.Replace(body, @"\[color=[^\]]+\](.*?)\[/color\]", "$1");

        // Convert bold, italic, and bullet points
        body = Regex.Replace(body, @"\[italic\](.*?)\[/italic\]", "*$1*");
        body = Regex.Replace(body, @"\[bold\](.*?)\[/bold\]", "**$1**");
        body = Regex.Replace(body, @"\[bullet\](.*?)\[/bullet\]", "- $1");

        return body;
    }
}
