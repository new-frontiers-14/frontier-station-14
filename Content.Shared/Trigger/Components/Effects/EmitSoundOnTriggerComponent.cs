using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Frontier Fields
/// Will play a sound in PVS range when triggered.
/// If TargetUser is true, it will be played at their position.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EmitSoundOnTriggerComponent : BaseXOnTriggerComponent
{
    // The <see cref="SoundSpecifier"/> to play.
    [DataField(required: true), AutoNetworkedField]
    public SoundSpecifier? Sound;

    /// <summary>
    /// Audio parameters to use when playing the sound.
    /// </summary>
    [DataField, AutoNetworkedField]
    public AudioParams AudioParams = AudioParams.Default;
    // End Frontier Fields

    /// <summary>
    /// Play the sound at the position instead of being parented to the source entity.
    /// Useful if the entity is deleted after.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Positional;

    // Should this sound be predicted for the User?
    [DataField, AutoNetworkedField]
    public bool Predicted;
}
