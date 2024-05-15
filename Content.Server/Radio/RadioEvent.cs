using Content.Server.Corvax.Language;
using Content.Shared.Chat;
using Content.Shared.Radio;

namespace Content.Server.Radio;

[ByRefEvent]
public readonly record struct RadioReceiveEvent(LanguageMessage Message, EntityUid MessageSource, RadioChannelPrototype Channel, MsgChatMessage ChatMsg, EntityUid RadioSource, MsgChatMessage? LanguageChatMsg);

/// <summary>
/// Use this event to cancel sending message per receiver
/// </summary>
[ByRefEvent]
public record struct RadioReceiveAttemptEvent(RadioChannelPrototype Channel, EntityUid RadioSource, EntityUid RadioReceiver)
{
    public readonly RadioChannelPrototype Channel = Channel;
    public readonly EntityUid RadioSource = RadioSource;
    public readonly EntityUid RadioReceiver = RadioReceiver;
    public bool Cancelled = false;
}

/// <summary>
/// Use this event to cancel sending message to every receiver
/// </summary>
[ByRefEvent]
public record struct RadioSendAttemptEvent(RadioChannelPrototype Channel, EntityUid RadioSource)
{
    public readonly RadioChannelPrototype Channel = Channel;
    public readonly EntityUid RadioSource = RadioSource;
    public bool Cancelled = false;
}
