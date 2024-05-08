namespace Content.Server.Corvax.Language;

[RegisterComponent]
public sealed partial class LanguageSpeakerComponent : Component
{
    [DataField]
    public string? Language;
}
