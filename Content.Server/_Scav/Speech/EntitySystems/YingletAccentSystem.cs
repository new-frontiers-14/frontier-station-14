using System.Text.RegularExpressions;
using Content.Server.Speech.Components;

namespace Content.Server.Speech.EntitySystems;

public sealed class YingletAccentSystem : EntitySystem
{
    private static readonly Regex RegexLowerTh = new Regex("th{1,3}");
    private static readonly Regex RegexUpperTh = new Regex("Th{1,3}");
    private static readonly Regex RegexFullUpperTh = new Regex("TH{1,3}");

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<YingletAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, YingletAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        // zhis is fun
        message = RegexLowerTh.Replace(message, "zh");
        message = RegexUpperTh.Replace(message, "Zh");
        message = RegexFullUpperTh.Replace(message, "ZH");

        args.Message = message;
    }
}
