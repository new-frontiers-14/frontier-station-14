using Content.Shared.Chemistry.Components;
using Robust.Shared.Random;

namespace Content.Server._NF.Chemistry;

public sealed class RandomPillSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public const int MaxPillType = 21;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PillComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<PillComponent> ent, ref MapInitEvent componentInit)
    {
        if (ent.Comp.Random)
        {
            ent.Comp.PillType = (uint)_random.Next(MaxPillType);
            Dirty(ent);
        }
    }
}
