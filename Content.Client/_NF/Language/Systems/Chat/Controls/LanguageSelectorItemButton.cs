using Content.Client.Stylesheets;
using Content.Client.UserInterface.Systems.Chat;
using Content.Client.UserInterface.Systems.Chat.Controls;
using Content.Shared.Chat;
using Content.Shared.Language;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._NF.Language.Systems.Chat.Controls;

// Mostly copied from ChannelSelectorItemButton
public sealed class LanguageSelectorItemButton : Button
{
    public readonly LanguagePrototype Language;

    public bool IsHidden => Parent == null;

    public LanguageSelectorItemButton(LanguagePrototype language)
    {
        Language = language;
        AddStyleClass(StyleNano.StyleClassChatChannelSelectorButton);

        Text = LanguageSelectorButton.LanguageSelectorName(language, full: true);

        // var prefix = ChatUIController.ChannelPrefixes[selector];
        // if (prefix != default)
        //     Text = Loc.GetString("hud-chatbox-select-name-prefixed", ("name", Text), ("prefix", prefix));
    }
}
