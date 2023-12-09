using Content.Shared.Language;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.Language;

public sealed partial class LanguageSystem : SharedLanguageSystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    public void InitializeWindows()
    {
        SubscribeNetworkEvent<RequestLanguageMenuStateMessage>(OnLanguagesRequest);
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
}
