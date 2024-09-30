using Robust.Shared.Utility;

namespace Content.Server._NF.Station.Components;

[RegisterComponent]
public sealed partial class ExtraStationInformationComponent: Component
{
    [DataField]
    public ResPath IconPath = new("/Textures/Interface/Misc/beakerlarge.png");

    [DataField]
    public LocId StationSubtext = new("frontier-lobby-frontier-subtext");
}
