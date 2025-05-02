using Robust.Shared.Configuration;

namespace Content.Shared._NF.CCVar;

public sealed partial class NFCCVars
{
    /// <summary>
    /// Starting balance for a new character
    /// </summary>
    public const int CompileTimeDefaultBalance = 50000;
    public static readonly CVarDef<int> DefaultBalance =
        CVarDef.Create("nf14.default.balance", CompileTimeDefaultBalance, CVar.SERVERONLY);
}
