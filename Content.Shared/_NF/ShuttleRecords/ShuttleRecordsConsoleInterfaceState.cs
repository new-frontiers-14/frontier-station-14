using Robust.Shared.Serialization;

namespace Content.Shared._NF.ShuttleRecords;

[Serializable, NetSerializable]
public sealed class ShuttleRecordsConsoleInterfaceState(
    List<ShuttleRecord> records
): BoundUserInterfaceState
{
    public List<ShuttleRecord> Records { get; set; } = records;
}
