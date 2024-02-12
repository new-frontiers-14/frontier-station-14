using Robust.Shared.GameStates;

namespace Content.Shared.StepTrigger.Components;

/// <summary>
/// Frontier - This is used for immunity to cancelling step trigger events if the user is wearing shoes, such as for glass shards.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ShoesRequiredStepTriggerImmuneComponent : Component  // Frontier
{
}
