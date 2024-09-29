using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Shared._NF.Smuggling.Prototypes;

// Data types for the sending of smuggling messages over radio.
[Prototype("smugglingReportMessageSet")]
public sealed class SmugglingReportMessageSetPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;
    // The radio channel to send this message off to.
    [DataField(required: true)]
    public ProtoId<RadioChannelPrototype> Channel;
    // The sets of messages to be sent off on this radio channel when a smuggling dead drop is taken.
    [DataField(required: true)]
    public List<SmugglingReportMessageSet> MessageSets { get; private set; } = new();
}

[DataDefinition]
public sealed partial class SmugglingReportMessageSet
{
    // The minimum delay, in minutes, that this message will be sent at.
    [DataField]
    public float MinDelay { get; private set; } = 0.0f;
    // The maximum delay, in minutes, that this message will be sent at.
    [DataField]
    public float MaxDelay { get; private set; } = 0.0f;
    // The list of messages to be sent.  The first message in the list whose threshold is met will be sent off.
    [DataField(required: true)]
    public List<SmugglingReportMessage> Messages { get; private set; } = new();
    // The probability of sending a message from this set.
    [DataField("prob")]
    public float Probability { get; private set; } = 1.0f;
}

[DataDefinition]
public sealed partial class SmugglingReportMessage
{
    // The localization string of the message to be printed.
    [DataField(required: true)]
    public string Message { get; private set; } = default!;
    // If the number of smuggling events this hour is lower than this value, this message will be printed off.
    [DataField]
    public int HourlyThreshold { get; private set; } = int.MaxValue;
    // The type of message to be printed off. Arguments should correspond to 
    [DataField]
    public SmugglingReportMessageType Type { get; private set; } = SmugglingReportMessageType.General;
    // The maximum error for the pod location in meters.  Printed locations will be a random location within X meters of the drop pod.
    [DataField("maxError")]
    public float MaxPodLocationError { get; private set; } = 0.0f;
}

public enum SmugglingReportMessageType : byte
{
    General, // No location.
    DeadDropStation, // Gives one station, the location of the dead drop. One arg: $location
    DeadDropStationWithRandomAlt, // Gives two stations, a random one of which had the dead drop. Two args: $location1, $location2
    PodLocation, // Gives the location of the drop pod. Two args: $x, $y
}
