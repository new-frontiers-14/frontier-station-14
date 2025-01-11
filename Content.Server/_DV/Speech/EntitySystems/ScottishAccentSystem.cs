using Content.Server._DV.Speech.Components;
using Content.Server.Speech;
using Content.Server.Speech.EntitySystems;
using System.Text.RegularExpressions;

namespace Content.Server._DV.Speech.EntitySystems;

public sealed class ScottishAccentSystem : EntitySystem
{
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScottishAccentComponent, AccentGetEvent>(OnAccentGet);
    }

    // converts left word when typed into the right word. For example typing you becomes ye.
    public string Accentuate(string message, ScottishAccentComponent component)
    {
        var msg = message;

        msg = _replacement.ApplyReplacements(msg, "scottish");

        return msg;
    }

    private void OnAccentGet(EntityUid uid, ScottishAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, component);
    }
}
