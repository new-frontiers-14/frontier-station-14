using Content.Shared._NF.CCVar;

namespace Content.Server._NF.River.Systems;

public partial class RiverNodeSystem
{
    public float UpdateRate { get; private set; }
    public int SegmentResolution { get; private set; }

    private void SubscribeCvars()
    {
        Subs.CVar(_cfg, NFCCVars.RiverUpdateRate, updateRate => UpdateRate = updateRate, true);
        Subs.CVar(_cfg, NFCCVars.ServerRiverResolution, segmentResolution => SegmentResolution = segmentResolution, true);
    }
}
