using Robust.Shared.Serialization;

namespace Content.Shared._NF.Radio;

[Serializable, NetSerializable]
public enum HandheldRadioUiKey : byte
{
    Key,
}

/// <summary>
/// The mode of the handheld radio microphone/speaker.
/// </summary>
[NetSerializable, Serializable]
public enum HandheldRadioMode : byte
{
    /// <summary>
    /// Speaker/Microphone is off
    /// </summary>
    Off = 0,

    /// <summary>
    /// Speaker/Microphone is on.
    /// 
    /// Speaker can only be heard if the radio is held in hand.
    /// The microphone can only be spoken into using the radio prefix in chat.
    /// </summary>
    Private,

    /// <summary>
    /// Speaker/Microphone is on.
    ///
    /// Speaker plays out loud, can be heard nearby.
    /// Microphone picks up everything in ListenRange. The player can also use the radio prefix to speak into the microphone.
    ///
    /// This is equivalent to the way that the old NC handheld radio
    /// (RadioMicrophoneComponent + RadioSpeakerComponent) works.
    /// </summary>
    Intercom
}

[Serializable, NetSerializable]
public sealed class HandheldRadioBoundUIState : BoundUserInterfaceState
{
    public HandheldRadioMode MicMode;
    public HandheldRadioMode SpeakerMode;
    public int Frequency;

    public HandheldRadioBoundUIState(HandheldRadioMode micMode, HandheldRadioMode speakerMode, int frequency)
    {
        MicMode = micMode;
        SpeakerMode = speakerMode;
        Frequency = frequency;
    }
}

[Serializable, NetSerializable]
public sealed class SetHandheldRadioMicModeMessage : BoundUserInterfaceMessage
{
    public HandheldRadioMode Mode;

    public SetHandheldRadioMicModeMessage(HandheldRadioMode mode)
    {
        Mode = mode;
    }
}

[Serializable, NetSerializable]
public sealed class SetHandheldRadioSpeakerModeMessage : BoundUserInterfaceMessage
{
    public HandheldRadioMode Mode;

    public SetHandheldRadioSpeakerModeMessage(HandheldRadioMode mode)
    {
        Mode = mode;
    }
}

[Serializable, NetSerializable]
public sealed class SelectHandheldRadioFrequencyMessage : BoundUserInterfaceMessage
{
    public int Frequency;

    public SelectHandheldRadioFrequencyMessage(int frequency)
    {
        Frequency = frequency;
    }
}
