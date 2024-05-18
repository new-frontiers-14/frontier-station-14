using Content.Shared.Speech;
using Robust.Shared.Prototypes;

namespace Content.Server.VoiceMask;

public enum RadioMode : byte // Frontier 
{
    Real,
    Fake,
    Unknown
}

[RegisterComponent]
public sealed partial class VoiceMaskerComponent : Component
{
    [DataField]
    public string LastSetName = "Unknown";

    [DataField]
    public ProtoId<SpeechVerbPrototype>? LastSpeechVerb;

    [DataField]
    public EntProtoId Action = "ActionChangeVoiceMask";

    [DataField]
    public EntityUid? ActionEntity;

    // Frontier
    [DataField]
    [AutoNetworkedField]
    public RadioMode RadioMode = RadioMode.Unknown;
}
