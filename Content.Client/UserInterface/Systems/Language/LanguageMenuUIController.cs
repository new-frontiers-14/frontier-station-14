using Content.Client.Gameplay;
using Content.Client.Language;
using Content.Shared.Language;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using static Content.Shared.Language.Systems.SharedLanguageSystem;

namespace Content.Client.UserInterface.Systems.Language;

public sealed class LanguageMenuUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
{
    public LanguageMenuWindow? _languageWindow;

    public override void Initialize()
    {
        EntityManager.EventBus.SubscribeLocalEvent<LanguageSpeakerComponent, LanguageMenuActionEvent>(OnActionMenu);
        EntityManager.EventBus.SubscribeEvent<LanguageMenuStateMessage>(EventSource.Network, this, OnStateUpdate);
    }

    private void OnStateUpdate(LanguageMenuStateMessage ev)
    {
        if (_languageWindow == null)
            return;

        _languageWindow.UpdateState(ev);
    }

    private void OnActionMenu(EntityUid uid, LanguageSpeakerComponent component, LanguageMenuActionEvent args)
    {
        if (_languageWindow == null)
            return;

        _languageWindow!.Open();

        EntityManager.EntityNetManager?.SendSystemNetworkMessage(new RequestLanguageMenuStateMessage());
    }

    public void OnStateEntered(GameplayState state)
    {
        _languageWindow = UIManager.CreateWindow<LanguageMenuWindow>();
        LayoutContainer.SetAnchorPreset(_languageWindow, LayoutContainer.LayoutPreset.Center);
    }

    public void OnStateExited(GameplayState state)
    {
        _languageWindow?.Dispose();
        _languageWindow = null;
    }
}
