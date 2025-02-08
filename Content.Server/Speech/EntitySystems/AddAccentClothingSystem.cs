using Content.Server.Speech.Components;
using Content.Shared.Clothing;
using Content.Shared.Verbs; // Frontier

namespace Content.Server.Speech.EntitySystems;

public sealed class AddAccentClothingSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AddAccentClothingComponent, ClothingGotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<AddAccentClothingComponent, ClothingGotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<AddAccentClothingComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs); // Frontier
    }

    private void OnGotEquipped(EntityUid uid, AddAccentClothingComponent component, ref ClothingGotEquippedEvent args)
    {
        // does the user already has this accent?
        var componentType = _componentFactory.GetRegistration(component.Accent).Type;
        if (HasComp(args.Wearer, componentType))
            return;

        // add accent to the user
        var accentComponent = (Component) _componentFactory.GetComponent(componentType);
        AddComp(args.Wearer, accentComponent);

        // snowflake case for replacement accent
        if (accentComponent is ReplacementAccentComponent rep)
            rep.Accent = component.ReplacementPrototype!;

        component.IsActive = true;
        component.Wearer = args.Wearer; // Frontier
    }

    private void OnGotUnequipped(EntityUid uid, AddAccentClothingComponent component, ref ClothingGotUnequippedEvent args)
    {
        component.Wearer = EntityUid.Invalid; // Frontier: prevent alt verb
        if (!component.IsActive)
            return;

        // try to remove accent
        var componentType = _componentFactory.GetRegistration(component.Accent).Type;
        if (EntityManager.HasComponent(args.Wearer, componentType))
        {
            EntityManager.RemoveComponent(args.Wearer, componentType);
        }

        component.IsActive = false;
    }

    // Frontier: togglable accents
    /// <summary>
    ///     Adds an alt verb allowing for the accent to be toggled easily.
    /// </summary>
    private void OnGetAltVerbs(EntityUid uid, AddAccentClothingComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || args.User != component.Wearer) //only the wearer can toggle the effect
            return;

        AlternativeVerb verb = new()
        {
            Text = Loc.GetString("accent-clothing-component-toggle"),
            Act = () => ToggleAccent(uid, component)
        };
        args.Verbs.Add(verb);
    }

    private void ToggleAccent(EntityUid uid, AddAccentClothingComponent component)
    {
        if (component.IsActive)
        {
            // try to remove the accent if it's enabled
            var componentType = _componentFactory.GetRegistration(component.Accent).Type;
            RemComp(component.Wearer, componentType);
            component.IsActive = false;
            // we don't wipe out wearer in this case
        }
        else
        {
            // try to add the accent as if we are equipping this item again
            // does the user already has this accent?
            var componentType = _componentFactory.GetRegistration(component.Accent).Type;
            if (HasComp(component.Wearer, componentType))
                return;

            // add accent to the user
            var accentComponent = (Component)_componentFactory.GetComponent(componentType);
            AddComp(component.Wearer, accentComponent);

            // snowflake case for replacement accent
            if (accentComponent is ReplacementAccentComponent rep)
                rep.Accent = component.ReplacementPrototype!;

            component.IsActive = true;
        }
    }
    // End Frontier: togglable accents
}
