using Content.Server._NF.Speech.Components;
using Content.Server.Actions;
using Content.Shared._NF.Speech.Events;
using Content.Server.Speech.Components;

namespace Content.Server._NF.Speech.EntitySystems;

//TODO: remove sleep deprived comments
/// <summary>
/// Allows for toggleable accents that are innate to the user instead of being tied to a specific item.
/// You can't have multiple toggleable accents at once (right now, refactor coming maybe in the future)
/// </summary>
public sealed class ToggleableAccentSystem : EntitySystem
{

    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IComponentFactory _componentFactory = default!;
    [Dependency] private ActionsSystem _actionsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ToggleableAccentComponent, ToggleAccentActionEvent>(OnToggleAction);
        SubscribeLocalEvent<ToggleableAccentComponent,ComponentRemove>(OnComponentRemove);
    }



    private void OnToggleAction(EntityUid uid, ToggleableAccentComponent component, ToggleAccentActionEvent actionEvent)
    {
        component.IsAccentActive = !component.IsAccentActive;
        actionEvent.Handled = true;
        actionEvent.Toggle = true;
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

    /// <remarks>
    /// If the component is removed for any reason, I want to ensure that the entity is left in a consistent state
    /// instead of it being dependent on the current status of the toggle
    /// </remarks>
    private void OnComponentRemove(EntityUid ent, ToggleableAccentComponent comp, ComponentRemove eventArgs)
    {
        switch (comp.RemovalBehavior)
        {
            case ToggleableAccentComponent.OnRemovalBehavior.ADD:
                ApplyAccent(ent, comp);
                break;
            case ToggleableAccentComponent.OnRemovalBehavior.REMOVE:
                RemoveAccent(ent, comp);
                break;
        }
        _actionsSystem.RemoveAction(ent, comp.ToggleAccentAction);

    }

    /// <summary>
    /// Makes an accent toggleable. Fails and returns false if the entity already has a toggleable accent.
    /// </summary>
    /// <param name="accentHolder">The entity to have the accent applied to</param>
    /// <param name="accentComp">The (already created, preferably already initalized) accent component</param>
    /// <param name="startActive">If the accent should start enabled or disabled</param>
    /// <param name="removalBehavior">What should happen if the ToggleableAccentComponent was removed</param>
    /// <param name="replacementAccentPrototypeId">If the accent is a ReplacementAccent, the id of the accent to use</param>
    /// <returns>True if the accent was successfully made toggleable, false if it failed due to the entity already having
    /// a toggleable accent</returns>
    public bool MakeAccentToggleable(EntityUid accentHolder,
        AccentBase accentComp,
        bool startActive,
        ToggleableAccentComponent.OnRemovalBehavior removalBehavior,
        string? replacementAccentPrototypeId = null)
    {
        if (HasComp<ToggleableAccentComponent>(accentHolder))
            return false;

        var compName = _componentFactory.GetRegistration(accentComp).Name;

        var newComp = _componentFactory.GetComponent<ToggleableAccentComponent>();
        newComp.AccentComponentName = compName;
        newComp.IsAccentActive = startActive;
        newComp.ReplacementAccentPrototypeName = replacementAccentPrototypeId;
        newComp.RemovalBehavior = removalBehavior;
        _entityManager.AddComponent(accentHolder, newComp);
        if (startActive)
        {
            ApplyAccent(accentHolder, newComp);
        }
        else
        {
            RemoveAccent(accentHolder, newComp);
        }
        var newAction = _actionsSystem.AddAction(accentHolder, newComp.ToggleAccentActionId);
        newComp.ToggleAccentAction = newAction;
        return true;
    }



}
