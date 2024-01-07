using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Language.Components;

public abstract partial class BaseTranslatorComponent : Component
{
    /// <summary>
    ///   The language this translator changes the speaker's language to when they don't specify one.
    ///   If null, does not modify the default language.
    /// </summary>
    [DataField("default-language")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? CurrentSpeechLanguage = null;

    /// <summary>
    ///   The list of additional languages this translator allows the wielder to speak.
    /// </summary>
    [DataField("spoken", customTypeSerializer: typeof(PrototypeIdListSerializer<LanguagePrototype>))]
    [ViewVariables(VVAccess.ReadWrite)]
    public List<string> SpokenLanguages = new();

    /// <summary>
    ///   The list of additional languages this translator allows the wielder to understand.
    /// </summary>
    [DataField("understood", customTypeSerializer: typeof(PrototypeIdListSerializer<LanguagePrototype>))]
    [ViewVariables(VVAccess.ReadWrite)]
    public List<string> UnderstoodLanguages = new();

    /// <summary>
    ///   The languages the wielding MUST know in order for this translator to have effect.
    ///   The field [RequiresAllLanguages] indicates whether all of them are required, or just one.
    /// </summary>
    [DataField("requires", customTypeSerializer: typeof(PrototypeIdListSerializer<LanguagePrototype>))]
    [ViewVariables(VVAccess.ReadWrite)]
    public List<string> RequiredLanguages = new();

    /// <summary>
    ///   If true, the wielder must understand all languages in [RequiredLanguages] to speak [SpokenLanguages],
    ///   and understand all languages in [RequiredLanguages] to understand [UnderstoodLanguages].
    ///
    ///   Otherwise, at least one language must be known (or the list must be empty).
    /// </summary>
    [DataField("requires-all")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool RequiresAllLanguages = false;

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

/// <summary>
///   Applied to entities who were injected with a translator implant.
/// </summary>
[RegisterComponent]
public sealed partial class ImplantedTranslatorComponent : IntrinsicTranslatorComponent
{
}
