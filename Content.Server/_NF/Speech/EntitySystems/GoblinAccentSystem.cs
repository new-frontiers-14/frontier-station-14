using Content.Server._NF.Speech.Components;
using Content.Server.Speech;
using Content.Server.Speech.EntitySystems;
using System.Text.RegularExpressions;

namespace Content.Server._NF.Speech.EntitySystems;

// The whole code is a copy of SouthernAccentSystem by UBlueberry (https://github.com/UBlueberry)
public sealed class GoblinAccentSystem : EntitySystem
{
    private static readonly Regex RegexIng = new(@"ing\b");
    private static readonly Regex RegexAnd = new(@"\band\b");
    private static readonly Regex RegexEr = new(@"er\b");
    private static readonly Regex RegexTt = new(@"tt");
    private static readonly Regex RegexOf = new(@"\bof\b");
    private static readonly Regex RegexTo = new(@"\bto\b");
    private static readonly Regex RegexThe = new(@"\bthe\b");
    private static readonly Regex RegexH = new(@"\bh");
    private static readonly Regex RegexSelf = new(@"self\b");
    private static readonly Regex RegexLe = new(@"le\b");
    private static readonly Regex RegexAll = new(@"all");
    private static readonly Regex RegexAl = new(@"al");
    private static readonly Regex RegexUl = new(@"ul");
    private static readonly Regex RegexEll = new(@"ell\b");
    private static readonly Regex RegexWl = new(@"wl");

    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GoblinAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, GoblinAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        message = _replacement.ApplyReplacements(message, "goblin");

        message = RegexIng.Replace(message, "in'");
        message = RegexAnd.Replace(message, "an'");
        message = RegexEr.Replace(message, "ah");
        message = RegexTt.Replace(message, "'");
        message = RegexH.Replace(message, "'");
        message = RegexSelf.Replace(message, "seuf");
        message = RegexOf.Replace(message, "o'");
        message = RegexTo.Replace(message, "ta");
        message = RegexThe.Replace(message, "da");
        message = RegexLe.Replace(message, "ow");
        message = RegexAll.Replace(message, "aw");
        message = RegexAl.Replace(message, "aw");
        message = RegexUl.Replace(message, "w");
        message = RegexEll.Replace(message, "ew");
        message = RegexWl.Replace(message, "w");

        args.Message = message;
    }
};
