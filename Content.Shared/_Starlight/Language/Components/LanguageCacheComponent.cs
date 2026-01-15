using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Language.Components;

// <summary>
//     Component that "caches" old languages a species had. useful for revertable language changes.
// </summary>
[RegisterComponent]
public sealed partial class LanguageCacheComponent : Component
{
    /// <summary>
    /// What languages did this entity *used* to speak
    /// </summary>
    [DataField]
    public HashSet<ProtoId<LanguagePrototype>>? SpeakingCache = null;

    /// <summary>
    /// What languages did this entity *used* to understand
    /// </summary>
    [DataField]
    public HashSet<ProtoId<LanguagePrototype>>? UnderstandingCache = null;

    /// <summary>
    /// Should <see cref="UniversalLanguageSpeakerComponent"/> be removed when restoring from this cache
    /// </summary>
    [DataField]
    public bool HasUniversal = false;
}
