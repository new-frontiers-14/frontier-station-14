using System.Text.Json.Serialization;

namespace Content.Server.Discord;

public struct WebhookMentions
{
    [JsonPropertyName("parse")]
    public HashSet<string> Parse { get; set; } = new();

    [JsonPropertyName("roles")] // Frontier: allow specific roles
    public HashSet<string> Roles { get; set; } = new(); // Frontier: allow specific roles

    public WebhookMentions()
    {
    }

    public void AllowRoleMentions()
    {
        Parse.Add("roles");
    }
}
