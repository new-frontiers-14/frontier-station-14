using System.Linq;
using Content.Shared._Starlight.Language.Components;

namespace Content.Shared._Starlight.Language.Systems;

public abstract partial class SharedLanguageSystem : EntitySystem
{
    /// <summary>
    /// Captures a <see cref="LanguageCacheComponent"/> for this entity and stores it there.
    /// </summary>
    /// <param name="ent">The entity the cache should be taken and applied to</param>
    public void CaptureCache(Entity<LanguageKnowledgeComponent> ent)
    {
        if (HasComp<LanguageCacheComponent>(ent))
            return; // The entity already has a cache which means languages were modified twice.

        var cache = EnsureComp<LanguageCacheComponent>(ent);
        cache.HasUniversal = HasComp<UniversalLanguageSpeakerComponent>(ent);
        cache.SpeakingCache = ent.Comp.SpokenLanguages.ToHashSet();
        cache.UnderstandingCache = ent.Comp.UnderstoodLanguages.ToHashSet();
    }

    /// <summary>
    /// restores a entites languages from their <see cref="LanguageCacheComponent"/>
    /// recomended to call UpdateLanguages after calling this.
    /// </summary>
    /// <param name="ent">The entity the cache should restored for</param>
    public void RestoreCache(Entity<LanguageCacheComponent> ent)
    {
        if (!TryComp<LanguageKnowledgeComponent>(ent, out var knowledge))
            return; // The ent had no knowledge to restore the cache into

        var cache = ent.Comp;
        if (cache.HasUniversal)
            EnsureComp<UniversalLanguageSpeakerComponent>(ent);
        else
            RemComp<UniversalLanguageSpeakerComponent>(ent);
        knowledge.SpokenLanguages = cache.SpeakingCache?.ToList() ?? knowledge.SpokenLanguages;
        knowledge.UnderstoodLanguages = cache.UnderstandingCache?.ToList() ?? knowledge.UnderstoodLanguages;
        RemComp<LanguageCacheComponent>(ent);
    }
}
