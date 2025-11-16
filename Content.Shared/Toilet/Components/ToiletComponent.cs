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

<<<<<<< HEAD

        /// <summary>
        /// Sound to play when toggling toilet seat.
        /// </summary>
        [DataField]
        public SoundSpecifier SeatSound = new SoundPathSpecifier("/Audio/Effects/toilet_seat_down.ogg");

        // Frontier: clog probability
        /// <summary>
        /// Sound to play when toggling toilet seat.
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
        SeatDown
    }
=======
    /// <summary>
    /// Sound to play when toggling toilet seat.
    /// </summary>
    [DataField]
    public SoundSpecifier SeatSound = new SoundPathSpecifier("/Audio/Effects/toilet_seat_down.ogg");
>>>>>>> 9f36a3b4ea321ca0cb8d0fa0f2a585b14d136d78
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
