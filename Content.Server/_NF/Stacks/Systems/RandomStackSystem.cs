using Content.Server._NF.Stacks.Components;
using Content.Server.Stack;
using Robust.Shared.Random;

namespace Content.Server._NF.Stacks.Systems;

public sealed class RandomStackSystem : EntitySystem
{
    [Dependency] private readonly StackSystem _stack = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RandomStackCountComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<RandomStackCountComponent> ent, ref ComponentInit init)
    {
        _stack.SetCount(ent, _random.Next(ent.Comp.Min, ent.Comp.Max + 1));
    }
}
