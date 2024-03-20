using Content.Server.Mind;
using Content.Shared.Language;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Robust.Shared.Player;

namespace Content.Server.Language;

public sealed partial class LanguageSystem
{
    [Dependency] private readonly MindSystem _mind = default!;

    public void InitializeNet()
    {
        // Refresh the client's state when its mind hops to a different entity
        SubscribeLocalEvent<MindContainerComponent, MindAddedMessage>((uid, _, _) => SendLanguageStateToClient(uid));
        SubscribeLocalEvent<MindComponent, MindGotRemovedEvent>((_, _, args) =>
        {
            if (args.Mind.Comp.Session != null)
                SendLanguageStateToClient(args.Mind.Comp.Session);
        });

        SubscribeLocalEvent<LanguageSpeakerComponent, LanguagesUpdateEvent>((uid, comp, _) => SendLanguageStateToClient(uid, comp));
        SubscribeNetworkEvent<RequestLanguagesMessage>((_, session) => SendLanguageStateToClient(session.SenderSession));
    }

    private void SendLanguageStateToClient(EntityUid uid, LanguageSpeakerComponent? comp = null)
    {
        // Try to find a mind inside the entity and notify its session
        if (!_mind.TryGetMind(uid, out var mind, out var mindComp) || mindComp.Session == null)
            return;

        SendLanguageStateToClient(uid, mindComp.Session, comp);
    }

    private void SendLanguageStateToClient(ICommonSession session, LanguageSpeakerComponent? comp = null)
    {
        // Try to find an entity associated with the session and resolve the languages from it
        if (session.AttachedEntity is not { Valid: true } entity)
            return;

        SendLanguageStateToClient(entity, session, comp);
    }

    private void SendLanguageStateToClient(EntityUid uid, ICommonSession session, LanguageSpeakerComponent? component = null)
    {
        var langs = GetLanguages(uid, component);
        if (langs == null)
            return;

        var message = new LanguagesUpdatedMessage(langs.CurrentLanguage, langs.SpokenLanguages, langs.UnderstoodLanguages);
        RaiseNetworkEvent(message, session);
    }
}
