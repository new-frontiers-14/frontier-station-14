using Content.Server._NF.Speech.Components;
using Content.Server.Actions;
using Content.Server.Popups;
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

    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly ActionsSystem _actionsSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    public readonly EntProtoId CognizineToggleActionPrototypeId = "ActionToggleCogniAccent";
    //There is one case where this would need to be accessed from another class, so I want to make sure we have as few magic strings
    //as possible
    public const string GenericToggleAccentPrototypeIdString = "ActionToggleAccent";
    public readonly EntProtoId GenericToggleAccentPrototypeId = GenericToggleAccentPrototypeIdString;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ToggleableAccentComponent, ToggleAccentActionEvent>(OnToggleAction);
        SubscribeLocalEvent<ToggleableAccentComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<ToggleableAccentComponent,ComponentRemove>(OnComponentRemove);
    }



    private void OnToggleAction(EntityUid uid, ToggleableAccentComponent component, ToggleAccentActionEvent actionEvent)
    {
        if (component.AccentComponentName == "" || component is { AccentComponentName: "ReplacementAccent", ReplacementAccentPrototypeName: "" })
        {
            //Component wasn't setup properly, give the user a notification and don't do anything else.
            _popupSystem.PopupCursor(Loc.GetString("toggleable-accent-misconfigured-popup"), uid);
            return;
        }
        //TODO: Confirm the Replacement Accent is valid
        //We might not actually need to do this as an invalid replacement accent simply wouldn't work

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

    /// <summary>
    /// Ran when the component is added to initialize the action and starting accent state.
    /// </summary>
    /// <remarks>
    /// I wanted to make sure that you could add this in YAML without it breaking, allowing entities to have a toggleable
    /// accent from the start. This handles everything that isn't adding the component or setting component values.
    /// </remarks>
    private void OnComponentInit(EntityUid ent, ToggleableAccentComponent comp, ComponentInit eventArts)
    {
        var newAction = _actionsSystem.AddAction(ent, comp.ActionPrototype);
        _actionsSystem.SetToggled(newAction, comp.IsAccentActive);
        comp.ToggleAccentAction = newAction;
        //If the component is added without proper YAML fill outs or via admin intervention, this field will be blank.
        //If it is, then there is no accent to toggle, so we just don't bother setting its initial state.
        if (comp.AccentComponentName == "")
            return;

        if (comp.IsAccentActive)
        {
            ApplyAccent(ent, comp);
        }
        else
        {
            RemoveAccent(ent, comp);
        }

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
        _actionsSystem.RemoveAction(ent, comp.ToggleAccentAction);
       //If the component was never setup properly, nothing but removal of the action needs to be done.
        if (comp.AccentComponentName == "")
            return;

        switch (comp.RemovalBehavior)
        {
            case ToggleableAccentComponent.OnRemovalBehavior.Add:
                ApplyAccent(ent, comp);
                break;
            case ToggleableAccentComponent.OnRemovalBehavior.Remove:
                RemoveAccent(ent, comp);
                break;
        }
    }

    /// <summary>
    /// Makes an accent toggleable. Fails and returns false if the entity already has a toggleable accent.
    /// </summary>
    /// <remarks>
    /// This only adds the component, through a convenient single method. All the logic and accent initialization happens
    /// in OnComponentInit
    /// </remarks>>
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
        newComp.ActionPrototype = actionProtoId;
        _entityManager.AddComponent(accentHolder, newComp);
        return true;
    }

    //Code taken from AccentClothingSystem, with light edits
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

    //Code taken from AccentClothingSystem, with light edits
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

}
