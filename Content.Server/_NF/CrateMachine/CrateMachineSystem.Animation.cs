using Content.Server.Power.Components;
using Content.Shared._NF.CrateMachine;
using AppearanceSystem = Robust.Server.GameObjects.AppearanceSystem;
using CrateMachineComponent = Content.Shared._NF.CrateMachine.Components.CrateMachineComponent;

namespace Content.Server._NF.CrateMachine;

/// <summary>
/// Handles starting the opening animation.
/// Updates the time remaining on the component.
/// </summary>
public sealed partial class CrateMachineSystem : SharedCrateMachineSystem
{
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;

    /// <summary>
    /// Keep track of time in this function, in order to process the animation.
    /// </summary>
    /// <param name="frameTime">The current frame time</param>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CrateMachineComponent, ApcPowerReceiverComponent>();
        while (query.MoveNext(out var uid, out var crateMachine, out var receiver))
        {
            if (!receiver.Powered)
                continue;

            ProcessOpeningAnimation(uid, frameTime, crateMachine);
            ProcessClosingAnimation(uid, frameTime, crateMachine);
        }
    }

    /// <summary>
    /// Updates the time remaining for the opening animation, calls the delegate when the animation finishes, and updates the visual state.
    /// </summary>
    /// <param name="uid">The Uid of the crate machine</param>
    /// <param name="frameTime">The current frame time</param>
    /// <param name="comp">The crate machine component</param>
    private void ProcessOpeningAnimation(EntityUid uid, float frameTime, CrateMachineComponent comp)
    {
        if (comp.OpeningTimeRemaining <= 0)
            return;

        comp.OpeningTimeRemaining -= frameTime;

        // Automatically start closing after it finishes open animation.
        if (comp.OpeningTimeRemaining <= 0)
        {
            comp.DidTakeCrate = false;
            RaiseLocalEvent(uid, new CrateMachineOpenedEvent(uid));
        }

        // Update at the end so the closing animation can start automatically.
        UpdateVisualState(uid, comp);
    }

    /// <summary>
    /// Updates the time remaining for the closing animation, calls the delegate when the animation finishes, and updates the visual state.
    /// </summary>
    /// <param name="uid">The Uid of the crate machine</param>
    /// <param name="frameTime">The current frame time</param>
    /// <param name="comp">The crate machine component</param>
    private void ProcessClosingAnimation(EntityUid uid, float frameTime, CrateMachineComponent comp)
    {
        if (!comp.DidTakeCrate && !IsOccupied(uid, comp, true))
        {
            comp.DidTakeCrate = true;
            comp.ClosingTimeRemaining = comp.ClosingTime;
        }

        comp.ClosingTimeRemaining -= frameTime;
        UpdateVisualState(uid, comp);
    }

    /// <summary>
    /// Updates the visual state of the crate machine by setting the visual state using the appearance system.
    /// </summary>
    /// <param name="uid">The Uid of the crate machine</param>
    /// <param name="component">The crate machine component</param>
    private void UpdateVisualState(EntityUid uid, CrateMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.OpeningTimeRemaining > 0)
            _appearanceSystem.SetData(uid, CrateMachineVisuals.VisualState, CrateMachineVisualState.Opening);
        else if (component.ClosingTimeRemaining > 0)
            _appearanceSystem.SetData(uid, CrateMachineVisuals.VisualState, CrateMachineVisualState.Closing);
        else if (!component.DidTakeCrate)
            _appearanceSystem.SetData(uid, CrateMachineVisuals.VisualState, CrateMachineVisualState.Open);
        else
            _appearanceSystem.SetData(uid, CrateMachineVisuals.VisualState, CrateMachineVisualState.Closed);
    }

    /// <summary>
    /// Starts the opening animation of the crate machine and calls the delegate when the animation finishes.
    /// </summary>
    /// <param name="crateMachineUid">The Uid of the crate machine</param>
    /// <param name="component">The crate machine component</param>
    public void OpenFor(EntityUid crateMachineUid, CrateMachineComponent component)
    {
        component.OpeningTimeRemaining = component.OpeningTime;
        UpdateVisualState(crateMachineUid, component);
    }
}
