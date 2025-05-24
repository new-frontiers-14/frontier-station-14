using Content.Client._NF.Tools.Components;
using Content.Client.Items;
using Content.Shared._NF.Item.ItemToggle.Components;

namespace Content.Client._NF.Tools.Systems;

public sealed class ComponentCyclerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        Subs.ItemStatus<ComponentCyclerComponent>(ent => new ComponentCyclerStatusControl(ent));
    }
}
