using Content.Shared.Chat;
using Content.Shared.Language;
using Content.Shared.Radio;

namespace Content.Server.Radio;

/// <summary>
/// <param name="UnderstoodChatMsg">The message to display when the speaker can understand "language"</param>
/// <param name="NotUnderstoodChatMsg">The message to display when the speaker cannot understand "language"</param>
/// </summary>
[ByRefEvent]
public readonly record struct RadioReceiveEvent(
    // Frontier - languages mechanic
    EntityUid MessageSource,
    RadioChannelPrototype Channel,
    ChatMessage UnderstoodChatMsg,
    ChatMessage NotUnderstoodChatMsg,
    LanguagePrototype Language
);

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
