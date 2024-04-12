namespace Content.Server._NF.Audio;

/// <summary>
/// Toggles <see cref="AmbientSoundComponent"/> and others when the entity dies using a MobState Dead.
/// </summary>
[RegisterComponent]
public sealed partial class SoundWhileAliveComponent : Component;
