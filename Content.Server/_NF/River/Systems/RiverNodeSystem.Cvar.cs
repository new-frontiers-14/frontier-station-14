using Content.Shared._NF.CCVar;

namespace Content.Server._NF.VoidRiver.Systems;

public partial class RiverNodeSystem
{
    public float UpdateRate { get; private set; }

    private void SubscribeCvars()
    {
        Subs.CVar(_cfg, NFCCVars.RiverUpdateRate, updateRate => UpdateRate = updateRate, true);
    }
}
