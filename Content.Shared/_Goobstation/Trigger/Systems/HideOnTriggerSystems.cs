using Content.Shared._Goobstation.Trigger;
using Content.Shared._Goobstation.Trigger.Components;
using Content.Shared.Trigger;
using Robust.Shared.GameObjects;

namespace Content.Shared._Goobstation.Trigger.Systems;

public sealed class HideOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HideOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(EntityUid uid, HideOnTriggerComponent component, TriggerEvent args)
    {
        _appearance.SetData(uid, HideOnTriggerVisuals.Hidden, true);
    }
}
