using Content.Shared._NF.Shipyard.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.Station.Components;

/// <summary>
/// The counterpart to ExtraStationInformationComponent - extra info to display on the latejoin crew tab.
/// </summary>
[RegisterComponent]
public sealed partial class ExtraShuttleInformationComponent : Component
{
    [DataField]
    public ProtoId<VesselPrototype>? Vessel;

    [DataField]
    public string Advertisement = string.Empty;

    [DataField]
    public bool HiddenWithoutOpenJobs;
}
