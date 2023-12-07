namespace Content.Shared.Language;

public abstract partial class BaseTranslatorComponent : Component
{
    /// <summary>
    ///   The language this translator changes the speaker's language to when they don't specify one.
    ///   If null, does not modify the default language.
    /// </summary>
    [DataField("current-language")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? CurrentSpeechLanguage = null;

    /// <summary>
    ///   The list of additional languages this translator allows the wielder to speak.
    /// </summary>
    [DataField("speaks")]
    [ViewVariables(VVAccess.ReadWrite)]
    public List<string> SpokenLanguages = new();

    /// <summary>
    ///   The list of additional languages this translator allows the wielder to understand.
    /// </summary>
    [DataField("understands")]
    [ViewVariables(VVAccess.ReadWrite)]
    public List<string> UnderstoodLanguages = new();

    [DataField("enabled")]
    public bool Enabled = true;
}

/// <summary>
///   A translator that must be held in a hand or a pocket of an entity in order ot have effect.
/// </summary>
[RegisterComponent]
public sealed partial class HandheldTranslatorComponent : BaseTranslatorComponent
{
    /// <summary>
    ///   Whether or not interacting with this translator
    ///   toggles it on or off.
    /// </summary>
    [DataField("toggleOnInteract")]
    public bool ToggleOnInteract = true;

    // TODO: not implemented
    // /// <summary>
    // ///   Whether or not this translator requires a power cell to work.
    // /// </summary>
    // [DataField("requiresPower")]
    // [ViewVariables(VVAccess.ReadWrite)]
    // public bool RequiresPower = true;
}

/// <summary>
///   A translator attached to an entity that translates its speech.
///   An example is a translator implant that allows the speaker to speak another language.
/// </summary>
[RegisterComponent, Virtual]
public partial class IntrinsicTranslatorComponent : BaseTranslatorComponent
{
}

/// <summary>
///   Applied internally to the holder of [HandheldTranslatorComponent].
///   Do not use directly. Use [HandheldTranslatorComponent] instead.
/// </summary>
[RegisterComponent]
public sealed partial class HoldsTranslatorComponent : IntrinsicTranslatorComponent
{
    public Component? Issuer = null;
}
