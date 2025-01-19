using Content.Server._NF.Speech.Components;
using Content.Server.Speech.Components;
using Content.Shared.Interaction.Events;
using Content.Shared._NF.Item;
using Content.Shared.Verbs;

namespace Content.Server._NF.Speech.EntitySystems;

public sealed class AddAccentPickupSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AddAccentPickupComponent, PickedUpEvent>(OnPickup);
        SubscribeLocalEvent<AddAccentPickupComponent, DroppedEvent>(OnDropped);
        SubscribeLocalEvent<AddAccentPickupComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);
    }

    private void OnPickup(EntityUid uid, AddAccentPickupComponent component, ref PickedUpEvent args)
    {
        // does the user already has this accent?
        var componentType = _componentFactory.GetRegistration(component.Accent).Type;
        if (HasComp(args.User, componentType))
            return;

        // add accent to the user
        var accentComponent = (Component)_componentFactory.GetComponent(componentType);
        AddComp(args.User, accentComponent);

        // snowflake case for replacement accent
        if (accentComponent is ReplacementAccentComponent rep)
            rep.Accent = component.ReplacementPrototype!;

        component.IsActive = true;
        component.Holder = args.User;
    }

    private void OnDropped(EntityUid uid, AddAccentPickupComponent component, DroppedEvent args)
    {
        component.Holder = EntityUid.Invalid; // prevent alt verb
        if (!component.IsActive)
            return;

        // try to remove accent
        var componentType = _componentFactory.GetRegistration(component.Accent).Type;
        RemComp(args.User, componentType);

        component.IsActive = false;
    }

    /// <summary>
    ///     Adds an alt verb allowing for the accent to be toggled easily.
    /// </summary>
    private void OnGetAltVerbs(EntityUid uid, AddAccentPickupComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || args.User != component.Holder) //only the holder can toggle the effect
            return;

        AlternativeVerb verb = new()
        {
            Text = Loc.GetString("accent-clothing-component-toggle"),
            Act = () => ToggleAccent(uid, component)
        };
        args.Verbs.Add(verb);
    }

    private void ToggleAccent(EntityUid uid, AddAccentPickupComponent component)
    {
        var componentType = _componentFactory.GetRegistration(component.Accent).Type;
        if (component.IsActive)
        {
            // try to remove the accent if it's enabled
            RemComp(component.Holder, componentType);
            component.IsActive = false;
            // we don't wipe out Holder in this case
        }
        else
        {
            // try to add the accent as if we are equipping this item again
            // does the user already has this accent?
            if (HasComp(component.Holder, componentType))
                return;

            // add accent to the user
            var accentComponent = (Component)_componentFactory.GetComponent(componentType);
            AddComp(component.Holder, accentComponent);

            // snowflake case for replacement accent
            if (accentComponent is ReplacementAccentComponent rep)
                rep.Accent = component.ReplacementPrototype!;

            component.IsActive = true;
        }
    }
}
