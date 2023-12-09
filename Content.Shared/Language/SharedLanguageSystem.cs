using Content.Shared.Actions;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared.Language;

public abstract class SharedLanguageSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    private static LanguagePrototype? _galacticCommon;
    private static LanguagePrototype? _universal;
    public static LanguagePrototype GalacticCommon { get => _galacticCommon!; }
    public static LanguagePrototype Universal { get => _universal!; }
    [Dependency] protected readonly IPrototypeManager _prototype = default!;
    [Dependency] protected readonly IRobustRandom _random = default!;
    protected ISawmill _sawmill = default!;

    public override void Initialize()
    {
        _galacticCommon = _prototype.Index<LanguagePrototype>("GalacticCommon");
        _universal = _prototype.Index<LanguagePrototype>("Universal");
        _sawmill = Logger.GetSawmill("language");

        SubscribeLocalEvent<LanguageSpeakerComponent, MapInitEvent>(OnInit);
    }

    public LanguagePrototype? GetLanguage(string id)
    {
        _prototype.TryIndex<LanguagePrototype>(id, out var proto);
        return proto;
    }

    private void OnInit(EntityUid uid, LanguageSpeakerComponent component, MapInitEvent args)
    {
        _action.AddAction(uid, ref component.Action, component.LanguageMenuAction, uid);
    }

    /// <summary>
    ///   Raised when on an entity when its list of languages changes.
    /// </summary>
    public sealed class LanguagesUpdateEvent : EntityEventArgs
    {
    }

    /// <summary>
    ///   Sent when a client wants to update its language menu.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class RequestLanguageMenuStateMessage : EntityEventArgs
    {
    }

    /// <summary>
    ///   Sent by the server when the client needs to update its language menu,
    ///   or directly after [RequestLanguageMenuStateMessage].
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class LanguageMenuStateMessage : EntityEventArgs
    {
        public string CurrentLanguage;
        public List<string> Options;

        public LanguageMenuStateMessage(string currentLanguage, List<string> options)
        {
            CurrentLanguage = currentLanguage;
            Options = options;
        }
    }
}
