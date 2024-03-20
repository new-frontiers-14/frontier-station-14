using Content.Shared.Actions;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared.Language.Systems;

public abstract class SharedLanguageSystem : EntitySystem
{
    [ValidatePrototypeId<LanguagePrototype>]
    public static readonly string GalacticCommonPrototype = "GalacticCommon";
    [ValidatePrototypeId<LanguagePrototype>]
    public static readonly string UniversalPrototype = "Universal";
    public static LanguagePrototype GalacticCommon { get; private set; } = default!;
    public static LanguagePrototype Universal { get; private set; } = default!;
    [Dependency] protected readonly IPrototypeManager _prototype = default!;
    [Dependency] protected readonly IRobustRandom _random = default!;
    protected ISawmill _sawmill = default!;

    public override void Initialize()
    {
        GalacticCommon = _prototype.Index<LanguagePrototype>("GalacticCommon");
        Universal = _prototype.Index<LanguagePrototype>("Universal");
        _sawmill = Logger.GetSawmill("language");
    }

    public LanguagePrototype? GetLanguage(string id)
    {
        _prototype.TryIndex<LanguagePrototype>(id, out var proto);
        return proto;
    }

    /// <summary>
    ///   Raised on an entity when its list of languages changes.
    /// </summary>
    public sealed class LanguagesUpdateEvent : EntityEventArgs
    {
    }

    /// <summary>
    ///   Sent from the client to the server when it needs to learn the list of languages its entity knows.
    ///   This event should always be followed by a <see cref="LanguagesUpdatedMessage"/>, unless the client doesn't have an entity.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class RequestLanguagesMessage : EntityEventArgs;

    /// <summary>
    ///   Sent to the client when its list of languages changes. The client should in turn update its HUD and relevant systems.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class LanguagesUpdatedMessage(string currentLanguage, List<string> spoken, List<string> understood) : EntityEventArgs
    {
        public string CurrentLanguage = currentLanguage;
        public List<string> Spoken = spoken;
        public List<string> Understood = understood;
    }
}
