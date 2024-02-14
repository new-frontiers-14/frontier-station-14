using Content.Client._NF.Language;
using Content.Client.Gameplay;
using Content.Shared.Language;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;
using static Content.Shared.Language.Systems.SharedLanguageSystem;

namespace Content.Client.UserInterface.Systems.Language;

public sealed class LanguageMenuUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
{
    public LanguageMenuWindow? _languageWindow;
    private TimeSpan _lastToggle = TimeSpan.Zero;

    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        IoCManager.InjectDependencies(this);
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
        if (_languageWindow == null || args.Handled || (_timing.RealTime - _lastToggle) < TimeSpan.FromMilliseconds(200))
            return;
        _lastToggle = _timing.RealTime; // For some reason, this event seems to be fired multiple times, causing flickering

        if (_languageWindow.IsOpen)
        {
            _languageWindow.Close();
        }
        else
        {
            _languageWindow!.Open();
            EntityManager.EntityNetManager?.SendSystemNetworkMessage(new RequestLanguageMenuStateMessage());
        }

        args.Handled = true;
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
