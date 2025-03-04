using Content.Shared._DV.Item.ItemToggle.Components;
using Content.Shared.Examine;
using Content.Shared.Item.ItemToggle;

namespace Content.Shared._DV.Item.ItemToggle.Systems;

public sealed class ItemToggleExamineSystem : EntitySystem
{
    [Dependency] private readonly ItemToggleSystem _toggle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemToggleExamineComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<ItemToggleExamineComponent> ent, ref ExaminedEvent args)
    {
        var msg = _toggle.IsActivated(ent.Owner) ? ent.Comp.On : ent.Comp.Off;
        args.PushMarkup(Loc.GetString(msg));
    }
}
