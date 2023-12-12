using Content.Shared.Examine;
using Content.Shared.Implants.Components;
using Content.Shared.Language.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Language.Systems;

public abstract class SharedTranslatorImplanterSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

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

    protected void OnAppearanceChange(EntityUid implanter, TranslatorImplanterComponent component)
    {
        var used = component.Used;
        _appearance.SetData(implanter, ImplanterVisuals.Full, !used);
    }
}
