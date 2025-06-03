using Content.Server.StationRecords.Systems;

namespace Content.Server.StationRecords;

// This component ensures the entity it is attached to does not have generic station records created for them.
//
[Access(typeof(StationRecordsSystem))]
[RegisterComponent]
public sealed partial class SpecialSectorStationRecordComponent : Component
{
    // Makes it so that a person with this won't create additional records in other places
    // Mainly used for antags syndicates so that they aren't suddenly in NFSD records and outed the minute they exist
    // Most commonly used on Syndicate
    [DataField]
    public RecordGenerationType RecordGeneration = RecordGenerationType.Normal;
}

[Flags]
public enum RecordGenerationType
{
    Normal, // This entity will have a normal sector record.
    FalseRecord, // This entity will have a sector record with falsified data (job, DNA, fingerprints)
    NoRecord, // This entity will not have a sector record.
}
