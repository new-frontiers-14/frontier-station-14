using System.Linq;

namespace Content.Server._UNEON.Subscriptions;

/// <summary>A last-confirmed snapshot, never extended by a failed request.</summary>
public sealed class UneonSubscriptionEntry
{
    public bool Linked { get; set; }
    public int Tier { get; set; }
    public string[] Permissions { get; set; } = Array.Empty<string>();
    public DateTimeOffset ConfirmedAt { get; set; }

    public bool IsValid(DateTimeOffset now, int graceMinutes)
    {
        var grace = Math.Clamp(graceMinutes, 0, 1440);
        return ConfirmedAt <= now && now - ConfirmedAt <= TimeSpan.FromMinutes(grace);
    }

    public bool Grants(string permission, DateTimeOffset now, int graceMinutes)
    {
        return Linked && Tier > 0 && IsValid(now, graceMinutes) && Permissions.Contains(permission);
    }
}
