using Content.Server._NF.Speech.Components;
using Content.Server.Speech.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;

namespace Content.Server._NF.Speech.EntitySystems;

public sealed class AddAccentPickupSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AddAccentPickupComponent, GettingPickedUpAttemptEvent>(OnPickup);
        SubscribeLocalEvent<AddAccentPickupComponent, DroppedEvent>(OnDropped);
    }

    private void OnPickup(EntityUid uid, AddAccentPickupComponent component, ref GettingPickedUpAttemptEvent args)
    {
        // does the user already has this accent?
        var componentType = _componentFactory.GetRegistration(component.Accent).Type;
        if (HasComp(args.User, componentType))
            return;

        // add accent to the user
        var accentComponent = (Component) _componentFactory.GetComponent(componentType);
        AddComp(args.User, accentComponent);

        // snowflake case for replacement accent
        if (accentComponent is ReplacementAccentComponent rep)
            rep.Accent = component.ReplacementPrototype!;

        component.IsActive = true;
    }

    private void OnDropped(EntityUid uid, AddAccentPickupComponent component, DroppedEvent args)
    {
        if (!component.IsActive)
            return;

        // try to remove accent
        var componentType = _componentFactory.GetRegistration(component.Accent).Type;
        if (EntityManager.HasComponent(args.User, componentType))
        {
            EntityManager.RemoveComponent(args.User, componentType);
        }

        component.IsActive = false;
    }
}
