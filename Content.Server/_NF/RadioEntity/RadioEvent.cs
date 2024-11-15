using Content.Shared.Radio;

namespace Content.Server._NF.Radio;

/// <summary>
/// Use this event to transform radio messages before they're sent.
/// </summary>
[ByRefEvent]
public record struct RadioTransformMessageEvent(RadioChannelPrototype Channel, EntityUid RadioSource, string Name, string Message, EntityUid MessageSource)
{
    public readonly RadioChannelPrototype Channel = Channel;
    public readonly EntityUid RadioSource = RadioSource;
    public string Name = Name;
    public string Message = Message;
    public EntityUid MessageSource = MessageSource;
}
