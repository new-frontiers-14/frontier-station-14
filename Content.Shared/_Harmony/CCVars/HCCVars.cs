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
        CVarDef.Create("queue.enable", false, CVar.SERVER);
}
