using Content.Shared._NF.CCVar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client._NF.River;

public partial class RiverNodeSystem
{
    public int SegmentRenderResolution { get; private set; }

    private void SubscribeCvars()
    {
        Subs.CVar(_cfg, NFCCVars.ClientRiverRenderResolution, OnResolutionUpdate, true);
    }

    private void OnResolutionUpdate(int resolution)
    {
        SegmentRenderResolution = resolution;
        UpdateAllNodes();
    }
}
