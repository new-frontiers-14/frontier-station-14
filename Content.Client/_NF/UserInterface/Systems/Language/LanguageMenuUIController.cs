using Content.Client._NF.Language;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Systems.Chat;
using Content.Shared.Language;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Console;
using Robust.Shared.Timing;
using static Content.Shared.Language.Systems.SharedLanguageSystem;

namespace Content.Client.UserInterface.Systems.Language;

public sealed class LanguageMenuUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
{
    public LanguageMenuWindow? _languageWindow;
    private TimeSpan _lastToggle = TimeSpan.Zero;

    public string? LastPreferredLanguage;
    public Action<List<string>>? LanguagesChanged;

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IConsoleHost _consoleHost = default!;

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
        LanguagesChanged?.Invoke(ev.Options);
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
            RequestUpdate();
        }

        args.Handled = true;
    }

    public void RequestUpdate()
    {
        EntityManager.EntityNetManager?.SendSystemNetworkMessage(new RequestLanguageMenuStateMessage());
    }

    public void SetLanguage(string id)
    {
        _consoleHost.ExecuteCommand("lsselectlang " + id);
        LastPreferredLanguage = id;
    }

    public void OnStateEntered(GameplayState state)
    {
        _languageWindow = UIManager.CreateWindow<LanguageMenuWindow>();
        _languageWindow.OnLanguageSelected += SetLanguage;
        LayoutContainer.SetAnchorPreset(_languageWindow, LayoutContainer.LayoutPreset.Center);
        RequestUpdate();
    }

    public void OnStateExited(GameplayState state)
    {
        _languageWindow?.Dispose();
        _languageWindow = null;
    }
}
