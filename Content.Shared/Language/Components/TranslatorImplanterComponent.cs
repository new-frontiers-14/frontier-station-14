using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Language.Components;

/// <summary>
///   An item that, when used on a mob, adds an intrinsic translator to it.
/// </summary>
[RegisterComponent]
public sealed partial class TranslatorImplanterComponent : Component
{
    [DataField("spoken", customTypeSerializer: typeof(PrototypeIdListSerializer<LanguagePrototype>)), ViewVariables]
    public List<string> SpokenLanguages = new();

    [DataField("understood", customTypeSerializer: typeof(PrototypeIdListSerializer<LanguagePrototype>)), ViewVariables]
    public List<string> UnderstoodLanguages = new();

    /// <summary>
    ///   The list of languages the mob must understand in order for this translator to have effect.
    ///   Knowing one language is enough.
    /// </summary>
    [DataField("requires", customTypeSerializer: typeof(PrototypeIdListSerializer<LanguagePrototype>)), ViewVariables]
    public List<string> RequiredLanguages = new();

    /// <summary>
    ///   If true, only allows to use this implanter on mobs.
    /// </summary>
    [DataField("mobs-only")]
    public bool MobsOnly = true;

    /// <summary>
    ///   Whether this implant has been used already.
    /// </summary>
    public bool Used = false;
}
