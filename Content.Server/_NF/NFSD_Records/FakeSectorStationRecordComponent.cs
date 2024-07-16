using Content.Server.StationRecords.Systems;

namespace Content.Server.StationRecords;

[Access(typeof(StationRecordsSystem))]
[RegisterComponent]
public sealed partial class FakeSectorStationRecordComponent : Component
{
    // Makes it so that a person with this will create additional records BUT the job is gonna be random between 3 most common types
    // Mainly used for antags like pirates where they won't be outed immiedietly, but can still try to effectively hide within
    // Most commonly used on Pirates
}
