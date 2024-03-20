using Content.Client.Language.Systems;
using Content.Client.UserInterface.Systems.Language;
using Content.Shared.Language;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client._NF.Language.Systems.Chat.Controls;

// Mostly copied from LanguageSelectorPopup
public sealed class LanguageSelectorPopup : Popup
{
    private readonly BoxContainer _channelSelectorHBox;
    private readonly Dictionary<string, LanguageSelectorItemButton> _selectorStates = new();

    public event Action<LanguagePrototype>? Selected;

    public LanguageSelectorPopup()
    {
        _channelSelectorHBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 1
        };

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
}
