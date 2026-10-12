using System.Linq;
using Content.Client.Resources;
using Content.Client.Stylesheets;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._NF.Shipyard.UI;

public sealed class AtmosOptionButton : OptionButton
{
    [Dependency] private readonly IResourceCache _resCache = default!;
    [Dependency] private readonly IStylesheetManager _stylesheetManager = default!;

    public AtmosOptionButton()
    {
        IoCManager.InjectDependencies(this);

        // Custom stylesheet for the shipyard console. We can either change several of upstream's UI classes to make
        // sure the style only applies to one specific control, or we can just pseudo-inherit from StyleNano and do
        // precisely-scoped overrides here.
        // We're overriding the default control font selection with one that also falls back to NotoSansDisplay if
        // necessary, because that has some more glyphs that we really want to use (in particular, U+2082 SUBSCRIPT TWO,
        // commonly known as "₂").
        // While we could just add this fallback to the default font substitution list in StyleNano, not all UI elements
        // use that; in particular, rich text has its own custom fonts that don't support substitution at all, and we
        // can't fix rich text because it's part of RT instead of Content.
        // Since it's not immediately obvious which controls use rich text and which ones don't, we'll limit it to
        // exactly here (for now), as the text displayed here isn't player-editable.
        var notoSansFont = _resCache.GetFont(
            [
                "/Fonts/NotoSans/NotoSans-Regular.ttf",
                "/Fonts/NotoSans/NotoSansSymbols-Regular.ttf",
                "/Fonts/NotoSans/NotoSansSymbols2-Regular.ttf",
                "/Fonts/NotoSansDisplay/NotoSansDisplay-Regular.ttf",
            ],
            size: 12);

        Stylesheet = new Stylesheet(
            _stylesheetManager.SheetNano.Rules.Concat([
                    new StyleRule(new SelectorElement(typeof(Label), null, null, null),
                    [
                        new StyleProperty(Label.StylePropertyFont, notoSansFont),
                    ]),
                ])
                .ToList()
        );
    }

    public override void ButtonOverride(Button button)
    {
        base.ButtonOverride(button);
        // The popup isn't a child of this control, and thus doesn't automatically apply its style sheet.
        // OptionButton *probably* should forcibly set the popup's stylesheet to the one that OptionButton is using,
        // since the popup being in an entirely separate control tree is an implementation detail. But it doesn't, and
        // we can't fix OptionButton because it's part of RT instead of Content.
        button.Stylesheet = Stylesheet;
    }
}
