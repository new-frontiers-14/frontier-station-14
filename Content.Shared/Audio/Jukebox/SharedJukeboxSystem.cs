using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization; // Frontier

namespace Content.Shared.Audio.Jukebox;

public abstract class SharedJukeboxSystem : EntitySystem
{
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
}
// Frontier: Interface State for Shuffle & Replay buttons.
[Serializable, NetSerializable]
public sealed class JukeboxInterfaceState (
    bool isReplaySelected,
    bool isShuffleSelected
) : BoundUserInterfaceState
{
    public bool IsReplaySelected { get; set; } = isReplaySelected;
    public bool IsShuffleSelected { get; set; } = isShuffleSelected;
}
// End Frontier
