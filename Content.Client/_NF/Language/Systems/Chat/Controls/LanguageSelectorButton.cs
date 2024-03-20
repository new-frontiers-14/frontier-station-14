using System.Linq;
using System.Numerics;
using Content.Client.Language.Systems;
using Content.Client.UserInterface.Systems.Chat.Controls;
using Content.Client.UserInterface.Systems.Language;
using Content.Shared.Language;
using Robust.Client.UserInterface;
using Robust.Shared.Utility;

namespace Content.Client._NF.Language.Systems.Chat.Controls;

// Mostly copied from ChannelSelectorButton
public sealed class LanguageSelectorButton : ChatPopupButton<LanguageSelectorPopup>
{
    public LanguagePrototype? SelectedLanguage { get; private set; }

    private const int SelectorDropdownOffset = 38;

    public LanguageSelectorButton()
    {
        Name = "LanguageSelector";

        Popup.Selected += Select;

        if (Popup.FirstLanguage is { } firstSelector)
        {
            Select(firstSelector);
        }

        IoCManager.Resolve<IUserInterfaceManager>().GetUIController<LanguageMenuUIController>().LanguagesUpdatedHook += UpdateLanguage;
    }

    private void UpdateLanguage((string current, List<string> spoken, List<string> understood) args)
    {
        Popup.SetLanguages(args.spoken);

        // Kill me please
        SelectedLanguage = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<LanguageSystem>().GetLanguage(args.current);
        Text = LanguageSelectorName(SelectedLanguage!);
    }

    protected override UIBox2 GetPopupPosition()
    {
        var globalLeft = GlobalPosition.X;
        var globalBot = GlobalPosition.Y + Height;
        return UIBox2.FromDimensions(
            new Vector2(globalLeft, globalBot),
            new Vector2(SizeBox.Width, SelectorDropdownOffset));
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
        IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<LanguageSystem>().RequestSetLanguage(language);

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
