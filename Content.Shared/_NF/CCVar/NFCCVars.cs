using Robust.Shared.Configuration;

namespace Content.Shared._NF.CCVar;

[CVarDefs]
public sealed class NFCCVars
{
    /*
     *  Respawn
    */
    /// <summary>
    /// Whether or not respawning is enabled.
    /// </summary>
    public static readonly CVarDef<bool> RespawnEnabled =
        CVarDef.Create("nf14.respawn.enabled", true, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Respawn time, how long the player has to wait in seconds after going into cryosleep. Should be small, misclicks happen.
    /// </summary>
    public static readonly CVarDef<float> RespawnCryoFirstTime =
        CVarDef.Create("nf14.respawn.cryo_first_time", 20.0f, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Respawn time, how long the player has to wait in seconds after death, or on subsequent cryo attempts.
    /// </summary>
    public static readonly CVarDef<float> RespawnTime =
        CVarDef.Create("nf14.respawn.time", 1200.0f, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Whether or not returning from cryosleep is enabled.
    /// </summary>
    public static readonly CVarDef<bool> CryoReturnEnabled =
        CVarDef.Create("nf14.uncryo.enabled", true, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// The time in seconds after which a cryosleeping body is considered expired and can be deleted from the storage map.
    /// </summary>
    public static readonly CVarDef<float> CryoExpirationTime =
        CVarDef.Create("nf14.uncryo.maxtime", 180 * 60f, CVar.SERVER | CVar.REPLICATED);

    /*
     *  Game
     */
    /// <summary>
    /// If false, the game will not display the round's objectives in the summary window.
    /// </summary>
    public static readonly CVarDef<bool> GameShowObjectives =
        CVarDef.Create("nf14.game.showobjectives", false, CVar.ARCHIVE | CVar.SERVERONLY);

    /*
     *  Public Transit
     */
    /// <summary>
    /// Whether public transit is enabled.
    /// </summary>
    public static readonly CVarDef<bool> PublicTransit =
        CVarDef.Create("nf14.publictransit.enabled", true, CVar.SERVERONLY);

    /*
     *  World Gen
     */
    /// <summary>
    /// The number of Trade Stations to spawn in every round
    /// </summary>
    public static readonly CVarDef<int> MarketStations =
        CVarDef.Create("nf14.worldgen.market_stations", 1, CVar.SERVERONLY);

    /// <summary>
    /// The number of Cargo Depots to spawn in every round
    /// </summary>
    public static readonly CVarDef<int> CargoDepots =
        CVarDef.Create("nf14.worldgen.cargo_depots", 4, CVar.SERVERONLY);

    /// <summary>
    /// The number of Optional Points Of Interest to spawn in every round
    /// </summary>
    public static readonly CVarDef<int> OptionalStations =
        CVarDef.Create("nf14.worldgen.optional_stations", 6, CVar.SERVERONLY);

    /// <summary>
    /// The multiplier to add to distance spawning calculations for a smidge of server setting variance
    /// </summary>
    public static readonly CVarDef<float> POIDistanceModifier =
        CVarDef.Create("nf14.worldgen.distance_modifier", 1f, CVar.SERVERONLY);

    /// <summary>
    /// The maximum number of times to retry POI placement during world generation.
    /// </summary>
    public static readonly CVarDef<int> POIPlacementRetries =
        CVarDef.Create("nf14.worldgen.poi_placement_retries", 10, CVar.SERVERONLY);

    /*
    * Shipyard
    */
    /// <summary>
    /// Whether the Shipyard is enabled.
    /// </summary>
    public static readonly CVarDef<bool> Shipyard =
        CVarDef.Create("shuttle.shipyard", true, CVar.SERVERONLY);

    /// <summary>
    /// Base sell rate (multiplier: 0.95 = 95%)
    /// </summary>
    public static readonly CVarDef<float> ShipyardSellRate =
        CVarDef.Create("shuttle.shipyard_base_sell_rate", 0.95f, CVar.SERVERONLY);

    /*
     * Salvage
     */
    /// <summary>
    /// The maximum number of shuttles able to go on expedition at once.
    /// </summary>
    public static readonly CVarDef<int> SalvageExpeditionMaxActive =
        CVarDef.Create("nf14.salvage.expedition_max_active", 15, CVar.REPLICATED);

    /// <summary>
    /// Cooldown for failed missions.
    /// </summary>
    public static readonly CVarDef<float> SalvageExpeditionFailedCooldown =
        CVarDef.Create("nf14.salvage.expedition_failed_cooldown", 1200f, CVar.REPLICATED);

    /// <summary>
    /// Transit time in hyperspace in seconds.
    /// </summary>
    public static readonly CVarDef<float> SalvageExpeditionTravelTime =
        CVarDef.Create("nf14.salvage.expedition_travel_time", 50f, CVar.REPLICATED);

    /// <summary>
    /// Whether or not to skip the expedition proximity check.
    /// </summary>
    public static readonly CVarDef<bool> SalvageExpeditionProximityCheck =
        CVarDef.Create("nf14.salvage.expedition_proximity_check", true, CVar.REPLICATED);

    /*
     * Smuggling
     */
    /// <summary>
    /// The maximum number of smuggling drop pods to be out at once.
    /// Taking another dead drop note will cause the oldest one to be destroyed.
    /// </summary>
    public static readonly CVarDef<int> SmugglingMaxSimultaneousPods =
        CVarDef.Create("nf14.smuggling.max_simultaneous_pods", 5, CVar.REPLICATED);
    /// <summary>
    /// The maximum number of dead drops (places to get smuggling notes) to place at once.
    /// </summary>
    public static readonly CVarDef<int> SmugglingMaxDeadDrops =
        CVarDef.Create("nf14.smuggling.max_sector_dead_drops", 10, CVar.REPLICATED);
    /// <summary>
    /// The minimum number of FUCs to spawn for anti-smuggling work.
    /// </summary>
    public static readonly CVarDef<int> SmugglingMinFucPayout =
        CVarDef.Create("nf14.smuggling.min_fuc_payout", 1, CVar.REPLICATED);
    /// <summary>
    /// The shortest time to wait before a dead drop spawns a new smuggling note.
    /// </summary>
    public static readonly CVarDef<int> DeadDropMinTimeout =
        CVarDef.Create("nf14.smuggling.min_timeout", 900, CVar.REPLICATED);
    /// <summary>
    /// The longest time to wait before a dead drop spawns a new smuggling note.
    /// </summary>
    public static readonly CVarDef<int> DeadDropMaxTimeout =
        CVarDef.Create("nf14.smuggling.max_timeout", 5400, CVar.REPLICATED);
    /// <summary>
    /// The shortest distance that a smuggling pod will spawn away from Frontier Outpost.
    /// </summary>
    public static readonly CVarDef<int> DeadDropMinDistance =
        CVarDef.Create("nf14.smuggling.min_distance", 6500, CVar.REPLICATED);
    /// <summary>
    /// The longest distance that a smuggling pod will spawn away from Frontier Outpost.
    /// </summary>
    public static readonly CVarDef<int> DeadDropMaxDistance =
        CVarDef.Create("nf14.smuggling.max_distance", 8000, CVar.REPLICATED);
    /// <summary>
    /// The smallest number of dead drop hints (paper clues to dead drop locations) at round start.
    /// </summary>
    public static readonly CVarDef<int> DeadDropMinHints =
        CVarDef.Create("nf14.smuggling.min_hints", 0, CVar.REPLICATED); // Used with BasicDeadDropHintVariationPass
    /// <summary>
    /// The largest number of dead drop hints (paper clues to dead drop locations) at round start.
    /// </summary>
    public static readonly CVarDef<int> DeadDropMaxHints =
        CVarDef.Create("nf14.smuggling.max_hints", 0, CVar.REPLICATED); // Used with BasicDeadDropHintVariationPass

    /*
    * Discord
    */
    /// <summary>
    ///     URL of the Discord webhook which will send round status notifications.
    /// </summary>
    public static readonly CVarDef<string> DiscordRoundWebhook =
        CVarDef.Create("discord.round_webhook", string.Empty, CVar.SERVERONLY);

    /// <summary>
    ///     Discord ID of role which will be pinged on new round start message.
    /// </summary>
    public static readonly CVarDef<string> DiscordRoundRoleId =
        CVarDef.Create("discord.round_roleid", string.Empty, CVar.SERVERONLY);

    /// <summary>
    ///     Send notifications only about a new round begins.
    /// </summary>
    public static readonly CVarDef<bool> DiscordRoundStartOnly =
        CVarDef.Create("discord.round_start_only", false, CVar.SERVERONLY);

    /// <summary>
    /// URL of the Discord webhook which will relay all round end messages.
    /// </summary>
    public static readonly CVarDef<string> DiscordLeaderboardWebhook =
        CVarDef.Create("discord.leaderboard_webhook", string.Empty, CVar.SERVERONLY);

    /*
    * Auth
    */
    public static readonly CVarDef<string> ServerAuthList =
        CVarDef.Create("frontier.auth_servers", "", CVar.CONFIDENTIAL | CVar.SERVERONLY);

    public static readonly CVarDef<bool> AllowMultiConnect =
        CVarDef.Create("frontier.allow_multi_connect", true, CVar.CONFIDENTIAL | CVar.SERVERONLY);

    /*
     * Events
     */
    /// <summary>
    ///     A scale factor applied to a grid's bounds when trying to find a spot to randomly generate a crate for bluespace events.
    /// </summary>
    public static readonly CVarDef<float> CrateGenerationGridBoundsScale =
        CVarDef.Create("nf14.events.crate_generation_grid_bounds_scale", 0.6f, CVar.SERVERONLY);

    /*
     * Atmos
     */
    /// <summary>
    ///     If true, allows map extraction (scrubbing a planet's atmosphere).
    /// </summary>
    public static readonly CVarDef<bool> AllowMapGasExtraction =
        CVarDef.Create("nf14.atmos.allow_map_gas_extraction", false, CVar.SERVER | CVar.REPLICATED);

    /*
     * Audio
     */

    /// <summary>
    /// The volume of expedition ending music.
    /// </summary>
    public static readonly CVarDef<float> SalvageExpeditionMusicVolume =
        CVarDef.Create("nf14.audio.expedition_music_volume", 0.50f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /*
     * Interface
     */

    /// <summary>
    /// If true, the admin overlay will display the players starting position.
    /// </summary>
    public static readonly CVarDef<bool> AdminOverlayBalance =
        CVarDef.Create("nf14.ui.admin_overlay_balance", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /*
     * Xenoarchaeology
     */

    /// <summary>
    /// If true, the admin overlay will display the players starting position.
    /// </summary>
    public static readonly CVarDef<bool> XenoarchSingleUseNodes =
        CVarDef.Create("nf14.xenoarch.single_use_nodes", true, CVar.REPLICATED);

    /*
     * Greeting
     */

    /// <summary>
    /// If true, enables a radio greeting whenever a new player spawns.
    /// </summary>
    public static readonly CVarDef<bool> NewPlayerRadioGreetingEnabled =
        CVarDef.Create("nf14.greeting.enabled", true, CVar.REPLICATED);

    /// <summary>
    /// The maximum playtime, in minutes, for a new player radio message to be sent.
    /// </summary>
    public static readonly CVarDef<int> NewPlayerRadioGreetingMaxPlaytime =
        CVarDef.Create("nf14.greeting.max_playtime", 180, CVar.REPLICATED); // Three hours

    /// <summary>
    /// The channel the radio message should be sent off on.
    /// </summary>
    public static readonly CVarDef<string> NewPlayerRadioGreetingChannel =
        CVarDef.Create("nf14.greeting.channel", "Service", CVar.REPLICATED);
}
