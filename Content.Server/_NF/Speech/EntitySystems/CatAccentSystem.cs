using Content.Server._NF.Speech.Components;
using Content.Server.Speech;
using Robust.Shared.Random;
using Content.Server.Speech.EntitySystems;
using System.Text.RegularExpressions;

namespace Content.Server._NF.Speech.EntitySystems;

public sealed class CatAccentSystem : EntitySystem
{
    private static readonly Regex RegexEr = new(@"(\w*[^pPfF])er\b", RegexOptions.IgnoreCase); // Words ending in "er"
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
        message = RegexErn.Replace(message, "$ewn"); // replace words ending with "ern" -> "ewn"
        message = RegexOr.Replace(message, "$ow"); // replace words ending with "or" -> "ow"

        // Replace 'r' with 'rrr' or 'w' based on random chance, while preserving case
        message = RegexR.Replace(message, match =>
        {
            // Check if the matched character is uppercase
            if (char.IsUpper(match.Value[0]))
            {
                return _random.Prob(0.5f) ? "RRR" : "W"; // Uppercase replacement
            }
            else
            {
                return _random.Prob(0.5f) ? "rrr" : "w"; // Lowercase replacement
            }
        });

        args.Message = message;
    }
}
