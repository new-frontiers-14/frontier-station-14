namespace Content.Server.Corvax.Language.Components;

[RegisterComponent]
public sealed partial class LanguageSpeakerComponent : Component
{
    [DataField]
    public string? Language;
}
