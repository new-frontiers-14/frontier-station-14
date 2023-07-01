using Robust.Shared.Configuration;

namespace Content.Shared.NF14.CCVar;

[CVarDefs]
public sealed class NF14CVars
{
    /// <summary>
    /// Whether or not respawning is enabled.
    /// </summary>
    public static readonly CVarDef<bool> RespawnEnabled =
        CVarDef.Create("nf14.respawn.enabled", true, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Respawn time, how long the player has to wait in seconds after death.
    /// </summary>
    public static readonly CVarDef<float> RespawnTime =
        CVarDef.Create("nf14.respawn.time", 600.0f, CVar.SERVER | CVar.REPLICATED);
}
