using Robust.Shared.Configuration;

namespace Content.Shared._Eclipse.CCVar;

[CVarDefs]
public sealed class EclipseCCVars
{
    public static readonly CVarDef<bool> RestartWhenServerEmpty =
        CVarDef.Create("eclipse.restart_when_server_empty", true, CVar.SERVERONLY);
}