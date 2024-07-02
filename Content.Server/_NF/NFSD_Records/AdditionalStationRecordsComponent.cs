using Content.Server.StationRecords.Systems;

namespace Content.Server.StationRecords;

[Access(typeof(StationRecordsSystem))]
[RegisterComponent]
public sealed partial class AdditionalStationRecordsComponent : Component
{
    // Determines if station should receive additional copy of records
    // Mainly used for places which require accurate records of every single player
    // Most commonly at NFSD outpost, possibly at coming Prison POI or even in Hospital POI if needed
}