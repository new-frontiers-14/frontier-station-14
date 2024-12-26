using Content.Shared.Chemistry.Components;
using Robust.Shared.Random;

namespace Content.Server._NF.Chemistry.EntitySystems;

public sealed class RandomPillSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;

    public const int MaxPill = 21;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PillComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, PillComponent component, ref ComponentInit componentInit)
    {
        if (component.Random)
        {
            component.PillType = (uint)_random.Next(MaxPill);
            Dirty(uid, component);
        }
    }
}
