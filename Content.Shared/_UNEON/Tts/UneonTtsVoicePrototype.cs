using Robust.Shared.Prototypes;

namespace Content.Shared._UNEON.Tts;

/// <summary>A selectable /N/TTS voice. The prototype ID is the provider's speaker ID.</summary>
[Prototype("uneonTtsVoice")]
public sealed partial class UneonTtsVoicePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name { get; private set; }
}
