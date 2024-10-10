using Content.Server.StationRecords.Systems;

namespace Content.Server.StationRecords;

[Access(typeof(StationRecordsSystem))]
[RegisterComponent]
public sealed partial class IgnoreSectorStationRecordComponent : Component
{
    // Makes it so that a person with this won't create additional records in other places
    // Mainly used for antags syndicates so that they aren't suddenly in NFSD records and outed the minute they exist
    // Most commonly used on Syndicate
}
