using Robust.Shared.Configuration;

namespace Content.Shared.DeltaV.CCVars;

/// <summary>
/// DeltaV specific cvars.
/// </summary>
[CVarDefs]
// ReSharper disable once InconsistentNaming - Shush you
public sealed class DCCVars
{
    /// <summary>
    /// Anti-EORG measure. Will add pacified to all players upon round end.
    /// Its not perfect, but gets the job done.
    /// </summary>
    public static readonly CVarDef<bool> RoundEndPacifist =
        CVarDef.Create("game.round_end_pacifist", false, CVar.SERVERONLY);

    /// <summary>
    /// Disables all vision filters for species like Vulpkanin or Harpies. There are good reasons someone might want to disable these.
    /// </summary>
    public static readonly CVarDef<bool> NoVisionFilters =
        CVarDef.Create("accessibility.no_vision_filters", false, CVar.CLIENTONLY | CVar.ARCHIVE);
}
