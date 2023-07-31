using Robust.Shared.Serialization;

namespace Content.Shared.StationRecords;

[Serializable, NetSerializable]
public sealed class GeneralStationRecordsFilter
{
    public GeneralStationRecordFilterType Type { get; set; }
        = GeneralStationRecordFilterType.Name;
    public string Value { get; set; } = "";
    public GeneralStationRecordsFilter(GeneralStationRecordFilterType filterType, string newValue = "")
    {
        Type = filterType;
        Value = newValue;
    }
}

[Serializable, NetSerializable]
public sealed class GeneralStationRecordsFilterMsg : BoundUserInterfaceMessage
{
    public string Value { get; }
    public GeneralStationRecordFilterType Type { get; }

    public GeneralStationRecordsFilterMsg(GeneralStationRecordFilterType filterType,
        string filterValue)
    {
        Type = filterType;
        Value = filterValue;
    }
}

[Serializable, NetSerializable]
public enum GeneralStationRecordFilterType : byte
{
    Name,
    Prints,
    DNA,
}

[Serializable, NetSerializable]
public sealed class AdjustStationJobMsg : BoundUserInterfaceMessage
{
    public string JobProto { get; }
    public int Amount { get; }

    public AdjustStationJobMsg(string jobProto, int amount)
    {
        JobProto = jobProto;
        Amount = amount;
    }
}
