using Content.Shared.Radio;
using Robust.Shared.Prototypes;

[Prototype("smugglingReportMessageSet")]
public sealed class SmugglingReportMessageSetPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;
    [DataField]
    public ProtoId<RadioChannelPrototype> Channel;
    [DataField]
    public List<SmugglingReportMessageSet> MessageSets { get; private set; } = new();
}

[Serializable]
public sealed class SmugglingReportMessageSet
{
    [DataField]
    public int MinDelay { get; private set; } = 0;
    [DataField]
    public int MaxDelay { get; private set; } = 0;
    [DataField]
    public List<SmugglingReportMessage> Messages { get; private set; } = new();
    [DataField("prob")]
    public float Probability { get; private set; } = 1.0f;
}

[Serializable]
public sealed class SmugglingReportMessage
{
    [DataField]
    public int HourlyThreshold { get; private set; } = int.MaxValue;
    [DataField]
    public string Message { get; private set; } = default!;
    [DataField]
    public SmugglingReportMessageType Type { get; private set; } = SmugglingReportMessageType.General;
}

public enum SmugglingReportMessageType : byte
{
    General, // No location.
    Precise, // Gives the exact location. One arg: $location
    Alternate, // Gives one of two locations. Two args: $location1, $location2
    PodLocation, // Gives the location of the drop pod. Two args: $x, $y
}
