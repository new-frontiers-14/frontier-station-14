namespace Content.Server.Corvax.Language;

[RegisterComponent]
public sealed partial class LanguageTranslatorComponent : Component
{
    public bool Activated = true;

    public EntityUid? ToggleActionEntity;
}
