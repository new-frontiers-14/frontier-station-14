using Content.Shared._NF.CrateMachine.Components;

namespace Content.Shared._NF.CrateMachine.Interfaces;

/// <summary>
/// Provides the ability to listen to events during various stages of the crate machine.
/// Typically used to spawn items when <see cref="OnCrateMachineOpened"/> is called.
/// </summary>
public interface ICrateMachineDelegate
{
    void OnCrateMachineOpened(EntityUid uid, CrateMachineComponent component) {}
    void OnCrateMachineClosed(EntityUid uid, CrateMachineComponent component) {}
    void OnCrateTaken(EntityUid uid, CrateMachineComponent component) {}
}
