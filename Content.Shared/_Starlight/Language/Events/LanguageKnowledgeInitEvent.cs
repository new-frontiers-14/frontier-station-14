using Content.Shared._Starlight.Language.Components;

namespace Content.Shared._Starlight.Language.Events;

/// <summary>
///     Fired as a broadcast event when a entity with language knowledge is map init'd
///     mainly used in the case of global language modifications being required
/// </summary>
[ByRefEvent]
public sealed class LanguageKnowledgeInitEvent(Entity<LanguageKnowledgeComponent> entity) : EntityEventArgs
{
    public Entity<LanguageKnowledgeComponent> Entity = entity;
}
