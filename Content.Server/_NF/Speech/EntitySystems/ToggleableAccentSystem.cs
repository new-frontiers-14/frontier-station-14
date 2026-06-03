using Content.Server._NF.Speech.Components;
using Content.Shared._NF.Speech.Events;
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

    /// <summary>
    /// Make an accent toggleable
    /// </summary>
    /// <param name="accentHolder">The entity to have the accent applied to</param>
    /// <param name="accentCompName">The (already created) accent component</param>
    /// <param name="startActive">If the accent should start enabled or disabled</param>
    /// <param name="replacementAccentId">If the accent is a ReplacementAccent, the id of the accent to use</param>
    /// <param name="removalBehavior">What should happen if the ToggleableAccentComponent was removed</param>
    public void MakeAccentToggleable(EntityUid accentHolder,
        string accentCompName,
        bool startActive,
        string? replacementAccentId = null,
        ToggleableAccentComponent.OnRemovalBehavior removalBehavior = ToggleableAccentComponent.OnRemovalBehavior.DEFAULT)
    {
        //TODO: Validate that you're passing an accent instead of doing something *really* stupid

    }



}
