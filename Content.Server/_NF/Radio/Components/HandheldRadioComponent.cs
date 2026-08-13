using Content.Server._NF.Radio.Systems;
using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.Radio;
using Content.Shared._NF.Radio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._NF.Radio.Components;

/// <summary>
///     Replacement for NC's handheld radio component.
///     Handles both sending/receiving messages.
/// </summary>
[RegisterComponent]
[Access(typeof(HandheldRadioSystem))]
public sealed partial class HandheldRadioComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("channel", customTypeSerializer: typeof(PrototypeIdSerializer<RadioChannelPrototype>))]
    public string Channel = SharedChatSystem.CommonChannel;

    /// <summary>
    // The radio frequency on which the message will be transmitted
    /// </summary>
    [DataField]
    public int Frequency = 1330;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("listenRange")]
    public int ListenRange = 4;

    /// <summary>
    /// Whether or not the speaker must have an
    /// unobstructed path to the radio to speak
    /// </summary>
    [DataField("unobstructedRequired")]
    public bool UnobstructedRequired = true;

    [DataField("microphoneMode")]
    public HandheldRadioMode MicrophoneMode = HandheldRadioMode.Private;

    [DataField("speakerMode")]
    public HandheldRadioMode SpeakerMode = HandheldRadioMode.Private;

    /// <summary>
    /// The output chat type when a message is played from the speaker in intercom mode.
    /// In other words, how loud the intercom speaker is.
    /// </summary>
    [DataField]
    public InGameICChatType OutputChatType = InGameICChatType.Whisper;
}
