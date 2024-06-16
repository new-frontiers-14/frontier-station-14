using Content.Server._NF.Speech.Components;
using Content.Server.Speech;
using Content.Server.Speech.EntitySystems;

namespace Content.Server._NF.Speech.EntitySystems;

public sealed class GoblinAccentSystem : EntitySystem
{
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GoblinAccentComponent, AccentGetEvent>(OnAccentGet);
    }

    // converts left word when typed into the right word. For example typing you becomes ye.
    public string Accentuate(string message, GoblinAccentComponent component)
    {
        var msg = message;

        msg = _replacement.ApplyReplacements(msg, "goblin");
        return msg;
    }

    private void OnAccentGet(EntityUid uid, GoblinAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message, component);
    }
}
