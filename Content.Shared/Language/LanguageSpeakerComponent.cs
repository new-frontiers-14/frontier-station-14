using Content.Shared.Actions;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
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
    ///     List of languages this entity can speak.
    /// </summary>
    [ViewVariables]
    [DataField("speaks", customTypeSerializer: typeof(PrototypeIdListSerializer<LanguagePrototype>), required: true)]
    public List<string> SpokenLanguages = new();

    /// <summary>
    ///     List of languages this entity can understand.
    /// </summary>
    [ViewVariables]
    [DataField("understands", customTypeSerializer: typeof(PrototypeIdListSerializer<LanguagePrototype>), required: true)]
    public List<string> UnderstoodLanguages = new();

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("languageMenuAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string LanguageMenuAction = "ActionLanguageMenu";

    [DataField] public EntityUid? Action;
}

[Serializable, NetSerializable]
public enum LanguageMenuUiKey : byte
{
    Key
}

public sealed partial class LanguageMenuActionEvent : InstantActionEvent { }
