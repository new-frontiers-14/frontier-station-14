using Content.Shared.Language;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.Language;

public sealed partial class LanguageSystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    public void InitializeWindows()
    {
        SubscribeNetworkEvent<RequestLanguageMenuStateMessage>(OnLanguagesRequest);
        SubscribeLocalEvent<LanguageSpeakerComponent, LanguagesUpdateEvent>(OnLanguageSwitch);
    }

    private void OnLanguagesRequest(RequestLanguageMenuStateMessage args, EntitySessionEventArgs session)
    {
        var uid = session.SenderSession.AttachedEntity;
        if (uid == null)
            return;

        var langs = GetLanguages(uid.Value);
        if (langs == null)
            return;

        var state = new LanguageMenuStateMessage(langs.CurrentLanguage, langs.SpokenLanguages);
        RaiseNetworkEvent(state, uid.Value);
    }

    private void OnLanguageSwitch(EntityUid uid, LanguageSpeakerComponent component, LanguagesUpdateEvent args)
    {
        var langs = GetLanguages(uid);
        if (langs == null)
            return;

        var state = new LanguageMenuStateMessage(langs.CurrentLanguage, langs.SpokenLanguages);
        RaiseNetworkEvent(state, uid);
    }
}
