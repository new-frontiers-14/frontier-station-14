using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization; // Frontier

namespace Content.Shared.Audio.Jukebox;

public abstract class SharedJukeboxSystem : EntitySystem
{
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
}

// Frontier: Shuffle & Repeat
[Serializable, NetSerializable]
public sealed class JukeboxInterfaceState(JukeboxPlaybackMode playbackMode) : BoundUserInterfaceState
{
    public JukeboxPlaybackMode PlaybackMode { get; set; } = playbackMode;
}
// End Frontier: Shuffle & Repeat
