using Robust.Shared.Serialization;

namespace Content.Shared._NF.ShuttleRecords;

[Serializable, NetSerializable]
public sealed class ShuttleRecordsConsoleInterfaceState(
    Dictionary<NetEntity, ShuttleRecord> records,
    int transactionCost
): BoundUserInterfaceState
{
    public Dictionary<NetEntity, ShuttleRecord> Records { get; set; } = records;
    public int TransactionCost { get; set; } = transactionCost;
}
