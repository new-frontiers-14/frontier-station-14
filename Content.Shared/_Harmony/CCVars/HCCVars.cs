using Robust.Shared.Configuration;

namespace Content.Shared._Harmony.CCVars;

/// <summary>
/// Harmony-specific cvars.
/// </summary>
[CVarDefs]
public sealed class HCCVars
{
    /// <summary>
    /// Modifies suicide command to ghost without killing the entity.
    /// </summary>
    public static readonly CVarDef<bool> DisableSuicide =
        CVarDef.Create("ic.disable_suicide", false, CVar.SERVER);

    /// <summary>
    /// Allows server hosters to turn the queue on and off
    /// </summary>
    public static readonly CVarDef<bool> EnableQueue =
        CVarDef.Create("queue.enable", true, CVar.SERVER);

  /// <summary>
    /// The maximum number of people that can be in the queue at a time.
    /// If this is set to 0, an infinite number of people can connect to the queue.
  /// </summary>
    public static readonly CVarDef<int> MaxQueuePlayerCount =
        CVarDef.Create("queue.max_player_count", 0, CVar.SERVERONLY);
}
