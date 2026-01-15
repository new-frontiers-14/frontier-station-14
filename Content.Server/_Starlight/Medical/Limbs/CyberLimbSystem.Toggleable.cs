using Content.Shared._Starlight.Medical.Limbs;
using Content.Shared.Actions.Components;
using Content.Shared.Body.Components;

namespace Content.Server._Starlight.Medical.Limbs;

public sealed partial class CyberLimbSystem : EntitySystem
{
    public void InitializeToggleable()
    {
        base.Initialize();
        SubscribeLocalEvent<BodyComponent, LimbAttachedEvent<IWithAction>>(IWithActionAttached);
        SubscribeLocalEvent<BodyComponent, LimbRemovedEvent<IWithAction>>(IWithActionRemoved);
    }

    private void IWithActionRemoved(Entity<BodyComponent> ent, ref LimbRemovedEvent<IWithAction> args)
        => _actions.RemoveProvidedActions(ent.Owner, args.Limb);

    private void IWithActionAttached(Entity<BodyComponent> ent, ref LimbAttachedEvent<IWithAction> args)
    {
        if (!TryComp(ent.Owner, out ActionsComponent? actions) ||
            !TryComp(args.Limb, out ActionsContainerComponent? container))
            return;

        _actions.GrantContainedActions((ent.Owner, actions), (args.Limb, container));
    }
}
