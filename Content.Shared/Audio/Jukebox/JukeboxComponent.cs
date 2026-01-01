using System.Numerics; // Frontier: wallmount jukebox
using System.Collections.Generic;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Audio.Jukebox;

[NetworkedComponent, RegisterComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedJukeboxSystem))]
public sealed partial class JukeboxComponent : Component
{
    /// <summary>
    /// The currently playing song.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<JukeboxPrototype>? SelectedSongId;

    /// <summary>
    /// The audiostream
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? AudioStream;

    /// <summary>
    /// The queue of queued songs.
    /// </summary>
    /// This is a LinkedList to allow for constant time insertion/deletion (vs a List), and more efficient
    /// moves (vs a Queue).
    /// It is also shamelessly stolen from the lathe queue implementation.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public LinkedList<ProtoId<JukeboxPrototype>> Queue = new();

    /// <summary>
    /// Whether or not a played song should be removed from the queue or readded to the bottom.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RepeatTracks;

    /// <summary>
    /// Whether or not the queue should be sampled randomly or in order.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ShuffleTracks;

    /// <summary>
    /// RSI state for the jukebox being on.
    /// </summary>
    [DataField]
    public string? OnState;

    /// <summary>
    /// RSI state for the jukebox being on.
    /// </summary>
    [DataField]
    public string? OffState;

    /// <summary>
    /// RSI state for the jukebox track being selected.
    /// </summary>
    [DataField]
    public string? SelectState;

    [ViewVariables]
    public bool Selecting;

    [ViewVariables]
    public float SelectAccumulator;

    // Frontier: wallmount jukebox
    [DataField]
    public Vector2 AudioOffset = Vector2.Zero;
    // End Frontier
}

[Serializable, NetSerializable]
public sealed class JukeboxPlayingMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class JukeboxPauseMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class JukeboxStopMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class JukeboxRepeatMessage(bool repeat) : BoundUserInterfaceMessage
{
    public bool Repeat { get; } = repeat;
}

[Serializable, NetSerializable]
public sealed class JukeboxShuffleMessage(bool shuffle) : BoundUserInterfaceMessage
{
    public bool Shuffle { get; } = shuffle;
}

[Serializable, NetSerializable]
public sealed class JukeboxSelectedMessage(ProtoId<JukeboxPrototype> songId) : BoundUserInterfaceMessage
{
    public ProtoId<JukeboxPrototype> SongId { get; } = songId;
}

[Serializable, NetSerializable]
public sealed class JukeboxSetTimeMessage(float songTime) : BoundUserInterfaceMessage
{
    public float SongTime { get; } = songTime;
}

// Component to raise an event when a track ends
[RegisterComponent]
public sealed partial class JukeboxMusicComponent : Component
{}

[Serializable, NetSerializable]
public enum JukeboxVisuals : byte
{
    VisualState
}

[Serializable, NetSerializable]
public enum JukeboxVisualState : byte
{
    On,
    Off,
    Select,
}

public enum JukeboxVisualLayers : byte
{
    Base
}
