using System.Reflection.Metadata.Ecma335;
using Content.Client.Language.Systems;
using Content.Client.UserInterface.Systems.Chat.Controls;
using Content.Client.UserInterface.Systems.Language;
using Content.Shared.Chat;
using Content.Shared.Language;
using Content.Shared.Language.Systems;
using Robust.Client.UserInterface.Controls;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client._NF.Language.Systems.Chat.Controls;

// Mostly copied from LanguageSelectorPopup
public sealed class LanguageSelectorPopup : Popup
{
    private readonly BoxContainer _channelSelectorHBox;
    private readonly Dictionary<string, LanguageSelectorItemButton> _selectorStates = new();
    private readonly LanguageMenuUIController _languageMenuController;

    public event Action<LanguagePrototype>? Selected;

    public LanguageSelectorPopup()
    {
        _channelSelectorHBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 1
        };

        _languageMenuController = UserInterfaceManager.GetUIController<LanguageMenuUIController>();
        _languageMenuController.LanguagesChanged += SetLanguages;

        AddChild(_channelSelectorHBox);
    }

    public LanguagePrototype? FirstLanguage
    {
        get
        {
            foreach (var selector in _selectorStates.Values)
            {
                if (!selector.IsHidden)
                    return selector.Language;
            }

            return null;
        }
    }

    private bool IsPreferredAvailable()
    {
        var preferred = _languageMenuController.LastPreferredLanguage;
        return preferred != null && _selectorStates.TryGetValue(preferred, out var selector) && !selector.IsHidden;
    }

    public void SetLanguages(List<string> languages)
    {
        var languageSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<LanguageSystem>();
        _channelSelectorHBox.RemoveAllChildren();

        foreach (var language in languages)
        {
            if (!_selectorStates.TryGetValue(language, out var selector))
            {
                var proto = languageSystem.GetLanguage(language);
                if (proto == null)
                    continue;

                selector = new LanguageSelectorItemButton(proto);
                _selectorStates[language] = selector;
                selector.OnPressed += OnSelectorPressed;
            }

            if (selector.IsHidden)
            {
                _channelSelectorHBox.AddChild(selector);
            }
        }

        var isPreferredAvailable = IsPreferredAvailable();
        if (!isPreferredAvailable)
        {
            var first = FirstLanguage;
            if (first != null)
                Select(first);
        }
    }

    private void OnSelectorPressed(ButtonEventArgs args)
    {
        var button = (LanguageSelectorItemButton) args.Button;
        Select(button.Language);
    }

    private void Select(LanguagePrototype language)
    {
        Selected?.Invoke(language);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _languageMenuController.LanguagesChanged -= SetLanguages;
    }
}
