using Content.Server._NF.Speech.Components;
using Content.Server._NF.Speech.Events;
using Linguini.Syntax.Ast;

namespace Content.Server._NF.Speech.EntitySystems;

//TODO: remove sleep deprived comments
/// <summary>
/// Allows for toggleable accents that are innate to the user instead of being tied to a specific item.
/// If you ever need to have two of these active at once (you can't), please reconsider your life choices
/// </summary>
public sealed class ToggleableAccentSystem : EntitySystem
{

    [Dependency] private IEntityManager _entMan = default!;

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

    //TODO: Look at plagiarizing code from AddAccentPickupSystem
    private void ApplyAccent(EntityUid target, ToggleableAccentComponent comp)
    {

    }

    private void RemoveAccent(EntityUid target, ToggleableAccentComponent comp)
    {

    }



}
