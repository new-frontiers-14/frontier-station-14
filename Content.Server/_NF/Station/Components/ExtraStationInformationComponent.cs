using Robust.Shared.Utility;

namespace Content.Server._NF.Station.Components;

[RegisterComponent]
public sealed partial class ExtraStationInformationComponent: Component
{
    [DataField]
    public ResPath? IconPath;

    [DataField]
    public LocId? StationSubtext;

    [DataField]
    public LocId? StationDescription;
}
