using Content.Client._NF.Language;
using Content.Client.Gameplay;
using Content.Client.Language.Systems;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Console;
using static Content.Shared.Language.Systems.SharedLanguageSystem;
using Content.Client.UserInterface.Controls;
using Content.Shared.Input;
using Content.Shared.Language;
using JetBrains.Annotations;
using Robust.Shared.Input.Binding;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.UserInterface.Systems.Language;

[UsedImplicitly]
public sealed class LanguageMenuUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
{
    public LanguageMenuWindow? _languageWindow;
    private MenuButton? LanguageButton => UIManager.GetActiveUIWidgetOrNull<MenuBar.Widgets.GameTopMenuBar>()?.LanguageButton;

    /// <summary>
    /// A hook similar to LanguageSystem.LanguagesUpdatedHook, but safe to use from ui code.
    /// This is a dirty workaround and I hate it.
    /// </summary>
    public Action<(string current, List<string> spoken, List<string> understood)>? LanguagesUpdatedHook;

    public override void Initialize()
    {
        LanguagesUpdatedHook += (args) =>
        {
            if (_languageWindow != null)
            {
                _languageWindow.UpdateState(args.current, args.spoken);
            }
        };
    }

    public void OnStateEntered(GameplayState state)
    {
        DebugTools.Assert(_languageWindow == null);

        var clientLanguageSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<LanguageSystem>();
        clientLanguageSystem.LanguagesUpdatedHook += LanguagesUpdatedHook;

        _languageWindow = UIManager.CreateWindow<LanguageMenuWindow>();
        LayoutContainer.SetAnchorPreset(_languageWindow, LayoutContainer.LayoutPreset.CenterTop);

        CommandBinds.Builder.Bind(ContentKeyFunctions.OpenLanguageMenu, InputCmdHandler.FromDelegate(_ => ToggleWindow())).Register<LanguageMenuUIController>();
    }

    public void OnStateExited(GameplayState state)
    {
        if (_languageWindow != null)
        {
            _languageWindow.Dispose();
            _languageWindow = null;
        }

        var clientLanguageSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<LanguageSystem>();
        clientLanguageSystem.LanguagesUpdatedHook -= LanguagesUpdatedHook;

        CommandBinds.Unregister<LanguageMenuUIController>();
    }

    public void UnloadButton()
    {
        if (LanguageButton == null)
        {
            return;
        }

        LanguageButton.OnPressed -= LanguageButtonPressed;
    }

    public void LoadButton()
    {
        if (LanguageButton == null)
        {
            return;
        }

        LanguageButton.OnPressed += LanguageButtonPressed;

        if (_languageWindow == null)
        {
            return;
        }

        _languageWindow.OnClose += DeactivateButton;
        _languageWindow.OnOpen += ActivateButton;
    }

    private void DeactivateButton() => LanguageButton!.Pressed = false;
    private void ActivateButton() => LanguageButton!.Pressed = true;

    private void LanguageButtonPressed(ButtonEventArgs args)
    {
        ToggleWindow();
    }

    private void CloseWindow()
    {
        _languageWindow?.Close();
    }

    private void ToggleWindow()
    {
        if (_languageWindow == null)
            return;

        if (LanguageButton != null)
        {
            LanguageButton.SetClickPressed(!_languageWindow.IsOpen);
        }

        if (_languageWindow.IsOpen)
        {
            CloseWindow();
        }
        else
        {
            _languageWindow.Open();
        }
    }
}
