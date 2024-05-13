using Robust.Shared.Serialization;

namespace Content.Shared.Corvax.Language.Components;

[RegisterComponent]
public sealed partial class LanguageTranslatorComponent : Component
{
    public bool Activated = true;

    public EntityUid? ToggleActionEntity;
}

[Serializable, NetSerializable]
public enum LanguageTranslatorVisuals
{
    Layer,
    Activated
}
