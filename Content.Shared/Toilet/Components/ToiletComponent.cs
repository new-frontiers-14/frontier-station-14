using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Toilet.Components;

/// <summary>
/// Seats that can toggled up and down with visuals to match.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ToiletComponent : Component
{
    /// <summary>
    /// Toggles seat state.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ToggleSeat;

    /// <summary>
    /// Sound to play when toggling toilet seat.
    /// </summary>
    [DataField]
    public SoundSpecifier SeatSound = new SoundPathSpecifier("/Audio/Effects/toilet_seat_down.ogg");

    // Frontier: clog probability
    /// <summary>
    /// Chance of being clogged upon mapinit.
    /// </summary>
    [DataField]
    public float ClogProbability = 0.0f;
    // End Frontier
}

[Serializable, NetSerializable]
public enum ToiletVisuals : byte
{
    SeatVisualState,
}

[Serializable, NetSerializable]
public enum SeatVisualState : byte
{
    SeatUp,
    SeatDown,
}
