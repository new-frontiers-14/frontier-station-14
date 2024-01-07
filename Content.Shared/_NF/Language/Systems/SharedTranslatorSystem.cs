using Content.Shared.Examine;
using Content.Shared.Language.Components;
using Content.Shared.Toggleable;

namespace Content.Shared.Language.Systems;

public abstract class SharedTranslatorSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandheldTranslatorComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, HandheldTranslatorComponent component, ExaminedEvent args)
    {
        var state = Loc.GetString(component.Enabled
            ? "translator-enabled"
            : "translator-disabled");

        args.PushMarkup(state);
    }

    protected void OnAppearanceChange(EntityUid translator, HandheldTranslatorComponent? comp = null)
    {
        if (comp == null && !TryComp(translator, out comp))
            return;

        _appearance.SetData(translator, ToggleVisuals.Toggled, comp.Enabled);
    }
}
