using Robust.Shared.Configuration;

namespace Content.Shared._Eclipse.CCVar;

[CVarDefs]
public sealed class EclipseCCVars
{
    public static readonly CVarDef<bool> RestartWhenServerEmpty =
        CVarDef.Create("eclipse.restart_when_server_empty", true, CVar.SERVERONLY);

    public static readonly CVarDef<bool> StartRoundWithNoPlayers =
        CVarDef.Create("eclipse.start_round_with_no_players", false, CVar.SERVERONLY);

    public static readonly CVarDef<string> DiscordInteractAlertWebhook =
        CVarDef.Create("eclipse.discord_interact_alert_webhook", string.Empty, CVar.SERVERONLY);
}