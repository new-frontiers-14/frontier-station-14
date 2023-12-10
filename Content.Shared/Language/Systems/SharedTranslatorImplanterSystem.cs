using Content.Shared.Examine;
using Content.Shared.Language.Components;

namespace Content.Shared.Language.Systems;

public abstract class SharedTranslatorImplanterSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TranslatorImplanterComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, TranslatorImplanterComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var text = !component.Used
            ? Loc.GetString("translator-implanter-ready")
            : Loc.GetString("translator-implanter-used");

        args.PushText(text);
    }
}
