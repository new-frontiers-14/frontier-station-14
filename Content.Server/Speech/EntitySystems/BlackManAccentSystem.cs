using System.Text;
using Content.Server.Speech.Components;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

public sealed class BlackManAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BlackManAccentComponent, AccentGetEvent>(OnAccent);
    }

    public string Accentuate(string message)
    {
        return "ХАЧУ БАНАН!";
    }

    private void OnAccent(EntityUid uid, BlackManAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message);
    }
}
