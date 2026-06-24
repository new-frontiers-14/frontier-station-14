using Content.Server._NF.Speech.Components;
using Content.Server.Actions;
using Content.Shared._NF.Speech.Events;
using Content.Server.Speech.Components;
using Content.Server.Speech.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._NF.Speech.EntitySystems;

/// <summary>
/// Allows for toggleable accents that are innate to the user instead of being tied to a specific item.
/// You can't have multiple toggleable accents at once, unless this system is refactored to allow that.
/// </summary>
public sealed class ToggleableAccentSystem : EntitySystem
{

    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IComponentFactory _componentFactory = default!;
    [Dependency] private ActionsSystem _actionsSystem = default!;

    public readonly EntProtoId CognizineToggleActionPrototypeId = "ActionToggleCogniAccent";
    public readonly EntProtoId GenericToggleAccentPrototypeId = "ActionToggleAccent";

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
    /// <summary>
    /// Applies a toggleable accent, ensuring that it is enabled by the time it returns.
    /// </summary>
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
    /// <summary>
    /// Removes an accent, ensuring it isn't on the target by the time it returns
    /// </summary>
    private void RemoveAccent(EntityUid target, ToggleableAccentComponent comp)
    {
        // try to remove accent
        var componentType = _componentFactory.GetRegistration(comp.AccentComponentName).Type;
        RemComp(target, componentType);

        comp.IsAccentActive = false;
    }

    ///<summary>
    /// Called when the component is removed to remove the action and finalize the accent according to component data.
    /// </summary>
    /// <remarks>
    /// If the component is removed for any reason, I want to ensure that the entity is left in a consistent state
    /// instead of it being dependent on the current status of the toggle
    /// </remarks>
    private void OnComponentRemove(EntityUid ent, ToggleableAccentComponent comp, ComponentRemove eventArgs)
    {
        switch (comp.RemovalBehavior)
        {
            case ToggleableAccentComponent.OnRemovalBehavior.Add:
                ApplyAccent(ent, comp);
                break;
            case ToggleableAccentComponent.OnRemovalBehavior.Remove:
                RemoveAccent(ent, comp);
                break;
        }
        _actionsSystem.RemoveAction(ent, comp.ToggleAccentAction);

    }

    /// <summary>
    /// Makes an accent toggleable. Fails and returns false if the entity already has a toggleable accent.
    /// </summary>
    /// <param name="accentHolder">The entity to have the accent applied to</param>
    /// <param name="accentComp">The (already created, preferably already initialized) accent component</param>
    /// <param name="startActive">If the accent should start enabled or disabled</param>
    /// <param name="removalBehavior">What should happen if the ToggleableAccentComponent was removed</param>
    /// <param name="actionProtoId">The action prototype to use. Use one of the readonly fields in this system.</param>
    /// <returns>True if the accent was successfully made toggleable, false if it failed due to the entity already having
    /// a toggleable accent</returns>
    public bool MakeAccentToggleable(EntityUid accentHolder,
        BaseAccentComponent accentComp,
        bool startActive,
        ToggleableAccentComponent.OnRemovalBehavior removalBehavior,
        EntProtoId actionProtoId)
    {
        if (HasComp<ToggleableAccentComponent>(accentHolder))
            return false;

        //Extract the replacement accent prototype as needed
        //Even if this is null, it won't matter, since it won't be touched in that case
        ProtoId<ReplacementAccentPrototype>? replacementAccentProtoId = null;
        if (accentComp is ReplacementAccentComponent replacementAccentComponent)
        {
            replacementAccentProtoId = replacementAccentComponent.Accent;
        }

        var compName = _componentFactory.GetRegistration(accentComp).Name;

        var newComp = _componentFactory.GetComponent<ToggleableAccentComponent>();
        newComp.AccentComponentName = compName;
        newComp.IsAccentActive = startActive;
        newComp.ReplacementAccentPrototypeName = replacementAccentProtoId;
        newComp.RemovalBehavior = removalBehavior;
        _entityManager.AddComponent(accentHolder, newComp);

        //TODO: Set the actions starting state according to this, or just cut that entirely, I don't know yet
        //TODO: Move this to an initialize event
        if (startActive)
        {
            ApplyAccent(accentHolder, newComp);
        }
        else
        {
            RemoveAccent(accentHolder, newComp);
        }
        var newAction = _actionsSystem.AddAction(accentHolder, actionProtoId);
        newComp.ToggleAccentAction = newAction;
        return true;
    }



}
