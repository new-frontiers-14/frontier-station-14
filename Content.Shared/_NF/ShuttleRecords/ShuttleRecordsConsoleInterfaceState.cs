using Robust.Shared.Serialization;

namespace Content.Shared._NF.ShuttleRecords;

[Serializable, NetSerializable]
public sealed class ShuttleRecordsConsoleInterfaceState(
    List<ShuttleRecord> records,
    int transactionCost
): BoundUserInterfaceState
{
    public List<ShuttleRecord> Records { get; set; } = records;
    public int TransactionCost { get; set; } = transactionCost;
}
