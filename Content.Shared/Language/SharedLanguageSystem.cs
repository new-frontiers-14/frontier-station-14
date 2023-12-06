using Content.Shared.Language;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared.Language;

public abstract class SharedLanguageSystem : EntitySystem
{
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
    }
}
