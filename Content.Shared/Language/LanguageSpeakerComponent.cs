using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Language;

[RegisterComponent, AutoGenerateComponentState]
public sealed partial class LanguageSpeakerComponent : Component
{
    /// <summary>
    ///  The current language the entity may use to speak.
    ///  Other listeners will hear the entity speak in this language.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public string CurrentLanguage = default!;

    /// <summary>
    ///     List of languages this entity can speak and understand.
    /// </summary>
    [ViewVariables]
    [DataField("languages", customTypeSerializer: typeof(PrototypeIdListSerializer<LanguagePrototype>), required: true)]
    public List<string> Languages = new();
}
