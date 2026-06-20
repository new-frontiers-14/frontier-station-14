using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Effects;

/// Frontier Fields
/// Will play a sound in PVS range when triggered.
/// If TargetUser is true, it will be played at their position.
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EmitSoundOnTriggerComponent : BaseXOnTriggerComponent
{
    /// The <see cref="SoundSpecifier"/> to play.
    [DataField(required: true), AutoNetworkedField]
    public SoundSpecifier? Sound;

    /// Frontier Fields
    /// <summary>
    /// Audio parameters to use when playing the sound.
    [DataField, AutoNetworkedField]
    public AudioParams AudioParams = AudioParams.Default;
    /// End Frontier Fields

    /// Play the sound at the position instead of being parented to the source entity.
    /// Useful if the entity is deleted after.
    [DataField, AutoNetworkedField]
    public bool Positional;

    /// Should this sound be predicted for the User?
    [DataField, AutoNetworkedField]
    public bool Predicted;
}
/// End Frontier Fields
