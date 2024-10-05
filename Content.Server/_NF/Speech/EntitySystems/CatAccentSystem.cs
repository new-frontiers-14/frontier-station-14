using Content.Server._NF.Speech.Components;
using Content.Server.Speech;
using Robust.Shared.Random;
using Content.Server.Speech.EntitySystems;
using System.Text.RegularExpressions;

namespace Content.Server._NF.Speech.EntitySystems;

public sealed class CatAccentSystem : EntitySystem
{
    private static readonly Regex RegexAn = new(@"\b(\w*an)\b", RegexOptions.IgnoreCase); // Words ending in "an"
    private static readonly Regex RegexEr = new(@"(\w*[^pPfF])er\b", RegexOptions.IgnoreCase); // Words ending in "er"
    private static readonly Regex RegexTion = new(@"\b(\w*tion)\b", RegexOptions.IgnoreCase); // Words ending in "tion"
    private static readonly Regex RegexErn = new(@"\b(\w*ern)\b", RegexOptions.IgnoreCase); // Words ending in "ern"
    private static readonly Regex RegexOr = new(@"\b(\w*or)\b", RegexOptions.IgnoreCase); // Words ending in "or"
    private static readonly Regex RegexR = new(@"r", RegexOptions.IgnoreCase); // Regex to match 'r'

    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CatAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, CatAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        message = _replacement.ApplyReplacements(message, "cat_accent");

        message = RegexAn.Replace(message, "$1nyan"); // Replace words ending with "an" -> "nyan"
        message = RegexTion.Replace(message, "$1nyation"); // Replace "tion" with "nyation"
        message = RegexErn.Replace(message, "ewn"); // replace words ending with "ern" -> "ewn"
        message = RegexOr.Replace(message, "ow"); // replace words ending with "or" -> "ow"

        foreach (Match match in RegexR.Matches(message))
            if (_random.Prob(0.5f))
                message = RegexR.Replace(message, "rrr"); // Replace 'r' with 'rrr' for purring effect
            else
                message = RegexR.Replace(message, "w");

        args.Message = message;
    }
}
