using Content.Shared._NF.ShuttleRecords;

namespace Content.Server._NF.ShuttleRecords.Components;

/// <summary>
/// A component that stores records for all shuttle purchases in the sector.
/// Note: all purchases are currently added, will need to be filtered appropriately by viewing clients.
/// </summary>
[RegisterComponent]
[Access(typeof(ShuttleRecordsSystem))]
public sealed partial class SectorShuttleRecordsComponent : Component
{
    [DataField]
    public List<ShuttleRecord> ShuttleRecordsList = [];
}
