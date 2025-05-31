using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._NF.Movement.Components;

/// <summary>
/// Frontier: play a rare sound on footstep
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RareFootstepModifierComponent : Component
{
    [DataField, AutoNetworkedField]
    public SoundSpecifier? FootstepSoundCollection;

    [DataField, AutoNetworkedField]
    public float Probability = 0.1f;
}
