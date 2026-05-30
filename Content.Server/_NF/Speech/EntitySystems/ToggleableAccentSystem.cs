using Content.Server._NF.Speech.Components;
using Content.Server._NF.Speech.Events;
using Content.Server.Speech.Components;
using Linguini.Syntax.Ast;

namespace Content.Server._NF.Speech.EntitySystems;

//TODO: remove sleep deprived comments
/// <summary>
/// Allows for toggleable accents that are innate to the user instead of being tied to a specific item.
/// If you ever need to have two of these active at once (you can't), please reconsider your life choices
/// </summary>
public sealed class ToggleableAccentSystem : EntitySystem
{

    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IComponentFactory _componentFactory = default!;

    //TODO: Is it possible for there to ever be two of this applied at once?
    //No, consider writing it to support multipule if we can figure out actions
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ToggleableAccentComponent, ToggleAccentActionEvent>(OnToggleAction);
    }



    private void OnToggleAction(EntityUid uid, ToggleableAccentComponent component, ToggleAccentActionEvent _)
    {
        component.IsAccentActive = !component.IsAccentActive;
        if (component.IsAccentActive)
        {
            ApplyAccent(uid, component);
        }
        else
        {
            RemoveAccent(uid, component);
        }
    }

    //Code stolen from AddAccentPickupSystem and lightly edited
    private void ApplyAccent(EntityUid target, ToggleableAccentComponent comp)
    {
        // does the user already has this accent?
        var componentType = _componentFactory.GetRegistration(comp.AccentComponentName).Type;
        if (HasComp(target, componentType))
            return;

        // add accent to the user
        var accentComponent = (Component)_componentFactory.GetComponent(componentType);
        AddComp(target, accentComponent);

        // special case for replacement accent
        if (accentComponent is ReplacementAccentComponent rep)
            rep.Accent = comp.ReplacementAccentPrototypeName!;

        comp.IsAccentActive = true;
    }

    //Code stolen from AddAccentPickupSystem with light edits
    private void RemoveAccent(EntityUid target, ToggleableAccentComponent comp)
    {
        // try to remove accent
        var componentType = _componentFactory.GetRegistration(comp.AccentComponentName).Type;
        RemComp(target, componentType);

        comp.IsAccentActive = false;
    }



}
