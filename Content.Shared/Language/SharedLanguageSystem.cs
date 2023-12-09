using Content.Shared.Actions;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

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
        _prototype.TryIndex("GalacticCommon", out LanguagePrototype? gc);
        _prototype.TryIndex("Universal", out LanguagePrototype? universal);
        _galacticCommon = gc;
        _universal = universal;
        _sawmill = Logger.GetSawmill("language");

        base.Initialize();

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

    public sealed class LanguageMenuState : BoundUserInterfaceState
    {
        public string CurrentLanguage;
        public List<string> Options;

        public LanguageMenuState(string currentLanguage, List<string> options)
        {
            CurrentLanguage = currentLanguage;
            Options = options;
        }
    }
}
