using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    public static readonly CVarDef<bool> UneonSubscriptionsEnabled =
        CVarDef.Create("uneon_subscriptions.enabled", false, CVar.SERVERONLY);

    public static readonly CVarDef<string> UneonSubscriptionsUrl =
        CVarDef.Create("uneon_subscriptions.api_url", "", CVar.SERVERONLY);

    public static readonly CVarDef<string> UneonSubscriptionsKey =
        CVarDef.Create("uneon_subscriptions.api_key", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

    // Last successful rights survive a temporary outage, but never indefinitely.
    public static readonly CVarDef<int> UneonSubscriptionsGraceMinutes =
        CVarDef.Create("uneon_subscriptions.grace_minutes", 360, CVar.SERVERONLY);
}
