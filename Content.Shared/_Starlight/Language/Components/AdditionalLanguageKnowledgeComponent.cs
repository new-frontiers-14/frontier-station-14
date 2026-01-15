using Content.Shared._Starlight.Language;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Language.Components;

/// <summary>
///     Has a list of languages that get added to an entity's LanguageKnowledgeComponent on init
/// </summary>
[RegisterComponent]
public sealed partial class AdditionalLanguageKnowledgeComponent : Component
{
    /// <summary>
    ///     List of languages this entity can speak without any external tools.
    /// </summary>
    [DataField("speaks")]
    public List<ProtoId<LanguagePrototype>> SpokenLanguages = new();

    /// <summary>
    ///     List of languages this entity can understand without any external tools.
    /// </summary>
    [DataField("understands")]
    public List<ProtoId<LanguagePrototype>> UnderstoodLanguages = new();
}
