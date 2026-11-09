using Content.Shared.Body.Systems;
using Content.Shared.Inventory;
using Content.Shared.Trigger.Components.Effects;
using Content.Shared.Body.Components; // Frontier

namespace Content.Shared.Trigger.Systems;

public sealed class GibOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GibOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<GibOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (target == null)
            return;

        if (ent.Comp.DeleteItems)
        {
            var items = _inventory.GetHandOrInventoryEntities(target.Value);
            foreach (var item in items)
            {
                PredictedQueueDel(item);
            }
        }

        // Frontier - Gib organs, conditional gibbing
        if (ent.Comp.DeleteOrgans)
        {
            if (TryComp<BodyComponent>(ent, out var body))
            {
                var organs = _body.GetBodyOrganEntityComps<TransformComponent>((ent, body));
                foreach (var organ in organs)
                {
                    Del(organ.Owner);
                }
            }
        }

        if (ent.Comp.Gib)
            _body.GibBody(target.Value, true);
        // End Frontier
        args.Handled = true;
    }
}