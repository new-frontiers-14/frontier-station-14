using Content.Server._NF.Speech.Components;
using Content.Server.Speech;
using Content.Server.Speech.EntitySystems;
using System.Text.RegularExpressions;

namespace Content.Server._NF.Speech.EntitySystems;

public sealed class CatAccentSystem : EntitySystem
{
    private static readonly Regex RegexAn = new(@"\b(\w*an)\b", RegexOptions.IgnoreCase); // Words ending in "an"
    private static readonly Regex RegexEr = new(@"(\w*[^pPfF])er\b", RegexOptions.IgnoreCase); // Words ending in "er"
    private static readonly Regex RegexTion = new(@"\b(\w*tion)\b", RegexOptions.IgnoreCase); // Words ending in "tion"
    private static readonly Regex RegexSion = new(@"\b(\w*sion)\b", RegexOptions.IgnoreCase); // Words ending in "sion"
    private static readonly Regex RegexR = new(@"r", RegexOptions.IgnoreCase); // Regex to match 'r'

    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

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
        message = RegexEr.Replace(message, "$1ah"); // Replace "er" with "ah"
        message = RegexTion.Replace(message, "$1nyation"); // Replace "tion" with "nyation"
        message = RegexSion.Replace(message, "$1nyation"); // Replace "sion" with "nyation"
        message = RegexR.Replace(message, "rrr"); // Replace 'r' with 'rrr' for purring effect

        args.Message = message;
    }
}
