using System.Linq;
using System.Numerics;
using Content.Client.UserInterface.Systems.Chat.Controls;
using Content.Shared.Language;
using Robust.Shared.Utility;

namespace Content.Client._NF.Language.Systems.Chat.Controls;

// Mostly copied from ChannelSelectorButton
public sealed class LanguageSelectorButton : ChatPopupButton<LanguageSelectorPopup>
{
    public event Action<LanguagePrototype>? OnLanguageSelect;

    public LanguagePrototype? SelectedLanguage { get; private set; }

    private const int SelectorDropdownOffset = 38;

    public LanguageSelectorButton()
    {
        Name = "LanguageSelector";

        Popup.Selected += OnLanguageSelected;

        if (Popup.FirstLanguage is { } firstSelector)
        {
            Select(firstSelector);
        }
    }

    protected override UIBox2 GetPopupPosition()
    {
        var globalLeft = GlobalPosition.X;
        var globalBot = GlobalPosition.Y + Height;
        return UIBox2.FromDimensions(
            new Vector2(globalLeft, globalBot),
            new Vector2(SizeBox.Width, SelectorDropdownOffset));
    }

    private void OnLanguageSelected(LanguagePrototype channel)
    {
        Select(channel);
    }

    public void Select(LanguagePrototype language)
    {
        if (Popup.Visible)
        {
            Popup.Close();
        }

        if (SelectedLanguage == language)
            return;
        SelectedLanguage = language;
        OnLanguageSelect?.Invoke(language);

        Text = LanguageSelectorName(language);
    }

    public static string LanguageSelectorName(LanguagePrototype language, bool full = false)
    {
        var name = language.LocalizedName;

        // if the language name is short enough, just return it
        if (full || name.Length < 5)
            return name;

        // If the language name is multi-word, collect first letters and capitalize them
        if (name.Contains(' '))
        {
            var result = name
                .Split(" ")
                .Select(it => it.FirstOrNull())
                .Where(it => it != null)
                .Select(it => char.ToUpper(it!.Value));

            return new string(result.ToArray());
        }

        // Alternatively, take the first 5 letters
        return name[..5];
    }

    // public Color ChannelSelectColor(ChatSelectChannel channel)
    // {
    //     return channel switch
    //     {
    //         ChatSelectChannel.Radio => Color.LimeGreen,
    //         ChatSelectChannel.LOOC => Color.MediumTurquoise,
    //         ChatSelectChannel.OOC => Color.LightSkyBlue,
    //         ChatSelectChannel.Dead => Color.MediumPurple,
    //         ChatSelectChannel.Admin => Color.HotPink,
    //         _ => Color.DarkGray
    //     };
    // }
}
