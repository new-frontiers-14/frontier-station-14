using Content.Shared.Language;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.Language;

public sealed partial class LanguageSystem : SharedLanguageSystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    public void InitializeWindows()
    {
        SubscribeLocalEvent<LanguageSpeakerComponent, LanguageMenuActionEvent>(MenuEvent);
    }

    private void MenuEvent(EntityUid uid, LanguageSpeakerComponent component, LanguageMenuActionEvent args)
    {
        if (!TryComp(uid, out ActorComponent? actor))
            return;

        EnsureComp<UserInterfaceComponent>(uid);

        // TODO: does not work
        _uiSystem.TryOpen(uid, LanguageMenuUiKey.Key, actor.PlayerSession);

       UpdateUserInterface(uid, component, actor.PlayerSession);
    }

    private void UpdateUserInterface(EntityUid uid, LanguageSpeakerComponent component, ICommonSession? session = null)
    {
        var langs = GetLanguages(uid, component);
        if (langs == null)
            return;

        if (session == null)
        {
            if (!TryComp<ActorComponent>(uid, out var actor))
                return;
            session = actor.PlayerSession;
        }

        var state = new LanguageMenuState(langs.CurrentLanguage, langs.SpokenLanguages);
        _uiSystem.TrySetUiState(uid, LanguageMenuUiKey.Key, state, session);
    }
}
