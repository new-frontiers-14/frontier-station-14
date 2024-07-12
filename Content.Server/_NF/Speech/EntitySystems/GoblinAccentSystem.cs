using Content.Server._NF.Speech.Components;
using Content.Server.Speech;
using Content.Server.Speech.EntitySystems;
using System.Text.RegularExpressions;

namespace Content.Server._NF.Speech.EntitySystems;

// The whole code is a copy of SouthernAccentSystem by UBlueberry (https://github.com/UBlueberry)
public sealed class GoblinAccentSystem : EntitySystem
{
    private static readonly Regex RegexIng = new(@"ing\b", RegexOptions.IgnoreCase);
    private static readonly Regex RegexAnd = new(@"\band\b", RegexOptions.IgnoreCase);
    private static readonly Regex RegexEr = new(@"er\b", RegexOptions.IgnoreCase);
    private static readonly Regex RegexTt = new(@"tt", RegexOptions.IgnoreCase);
    private static readonly Regex RegexOf = new(@"\bof\b", RegexOptions.IgnoreCase);
    private static readonly Regex RegexThe = new(@"\bthe\b", RegexOptions.IgnoreCase);
    private static readonly Regex RegexH = new(@"\bh", RegexOptions.IgnoreCase);
    private static readonly Regex RegexSelf = new(@"self\b", RegexOptions.IgnoreCase);

    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GoblinAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, GoblinAccentComponent component, AccentGetEvent args)
    {
        Log.Error($"OnAccent! {args.Message}");
        var message = args.Message;

        message = _replacement.ApplyReplacements(message, "goblin_accent");

        Log.Error(message);

        message = RegexIng.Replace(message, "in'");
        message = RegexAnd.Replace(message, "an'");
        message = RegexEr.Replace(message, "ah");
        message = RegexTt.Replace(message, "'");
        message = RegexH.Replace(message, "'");
        message = RegexSelf.Replace(message, "sewf");
        message = RegexOf.Replace(message, "o'");
        message = RegexThe.Replace(message, "da");

        Log.Error(message);

        args.Message = message;
    }
};
