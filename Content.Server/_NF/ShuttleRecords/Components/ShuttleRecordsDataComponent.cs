using Content.Shared._NF.ShuttleRecords;

namespace Content.Server._NF.ShuttleRecords.Components;

/// <summary>
/// Component that is put on the console's grid that will hold all things that are sold at cargo, for that grid.
/// </summary>
[RegisterComponent]
[Access(typeof(ShuttleRecordsSystem))]
public sealed partial class ShuttleRecordsDataComponent : Component
{
    [DataField]
    public List<ShuttleRecord> ShuttleRecordsList = [];
}
