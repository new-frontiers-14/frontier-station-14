using Robust.Shared.Prototypes;

namespace Content.Server._Starlight.Language;

[RegisterComponent]
public sealed partial class DebugLanguageChatterComponent : Component
{
    [DataField]
    public float IntervalSeconds = 1f;

    [DataField]
    public string MessageLocId = "debug-language-chatter-message";

    [DataField]
    public bool IncludeUniversal = false;

    [DataField]
    public bool Busy = false;
}
