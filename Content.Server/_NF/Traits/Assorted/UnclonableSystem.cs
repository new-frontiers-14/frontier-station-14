using Content.Shared.Cloning.Events;

namespace Content.Server._NF.Traits.Assorted;

public sealed class UnclonableSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UnclonableComponent, CloningAttemptEvent>(OnCloningAttempt);
    }

    private void OnCloningAttempt(Entity<UnclonableComponent> ent, ref CloningAttemptEvent args)
    {
        args.Cancelled = true;
    }
}
