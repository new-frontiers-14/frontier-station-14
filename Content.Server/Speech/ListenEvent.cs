using Content.Server.Corvax.Language;

namespace Content.Server.Speech;

public sealed class ListenEvent : EntityEventArgs
{
    public readonly LanguageMessage Message;
    public readonly EntityUid Source;

    public ListenEvent(LanguageMessage message, EntityUid source)
    {
        Message = message;
        Source = source;
    }
}

public sealed class ListenAttemptEvent : CancellableEntityEventArgs
{
    public readonly EntityUid Source;

    public ListenAttemptEvent(EntityUid source)
    {
        Source = source;
    }
}
