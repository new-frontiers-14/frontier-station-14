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

    /**
     * The order in which this station should be displayed in the lobby.
     * These are currently the set values, and as an example, this is how they are sorted:
     * 1 -- frontier station
     * 2 -- nfsd
     * 3 -- expedition lodge
     * 0 -- 'wysiwyg' and will be listed below sorted stations
     */
    [DataField]
    public int LobbySortOrder;

    /**
     * Determines if the station is a latejoin station option they can see in the latejoin menu.
     */
    [DataField]
    public bool IsLateJoinStation = true;
}
