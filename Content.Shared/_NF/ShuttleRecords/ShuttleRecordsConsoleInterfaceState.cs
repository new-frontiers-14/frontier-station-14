using Robust.Shared.Serialization;

namespace Content.Shared._NF.ShuttleRecords;

[Serializable, NetSerializable]
public sealed class ShuttleRecordsConsoleInterfaceState(
    List<ShuttleRecord>? records,
    int transactionCost,
    bool isTargetIdPresent,
    string? targetIdFullName,
    string? targetIdVesselName
) : BoundUserInterfaceState
{
    public bool IsTargetIdPresent { get; set; } = isTargetIdPresent;
    public List<ShuttleRecord>? Records { get; set; } = records; // To cut down on bandwidth, states without changes to records imply no change to the last state seen.
    public int TransactionCost { get; set; } = transactionCost;
    public string? TargetIdFullName { get; set; } = targetIdFullName;
    public string? TargetIdVesselName { get; set; } = targetIdVesselName;
}
