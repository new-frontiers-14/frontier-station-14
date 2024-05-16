using Content.Server.Speech.Components;
using Robust.Shared.Random;
using System.Text.RegularExpressions;

namespace Content.Server.Speech.EntitySystems;

public sealed class StreetpunkAccentSystem : EntitySystem
{
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;
    private static readonly Regex RegexIng = new(@"ing\b");
    private static readonly Regex RegexAnd = new(@"\band\b");
    private static readonly Regex RegexDve = new("d've");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StreetpunkAccentComponent, AccentGetEvent>(OnAccentGet);
    }

    // converts left word when typed into the right word. For example typing you becomes ye.
    public string Accentuate(string message, StreetpunkAccentComponent component)
    {
        var msg = message;

        //They shoulda started runnin' an' hidin' from me! <- bit from SouthernDrawl Accent
        msg = RegexIng.Replace(msg, "in'");
        msg = RegexAnd.Replace(msg, "an'");
        msg = RegexDve.Replace(msg, "da");

        msg = _replacement.ApplyReplacements(msg, "streetpunk");


        return msg;
    }

    private void OnAccentGet(EntityUid uid, StreetpunkAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, component);
    }
}
