using Content.Shared.Body.Systems;
using Content.Shared.Inventory;
using Content.Shared.Trigger.Components.Effects;

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
        _body.GibBody(target.Value, true);
        args.Handled = true;
    }
}

// Frontier upstream merge todo: add this to OnTrigger
/*
 // Frontier: more configurable gib triggers
        private void HandleGibTrigger(EntityUid uid, GibOnTriggerComponent component, TriggerEvent args)
        {
            EntityUid ent;
            if (component.UseArgumentEntity)
            {
                ent = uid;
            }
            else
            {
                if (!TryComp(uid, out TransformComponent? xform))
                    return;
                ent = xform.ParentUid;
            }

            if (component.DeleteItems)
            {
                var items = _inventory.GetHandOrInventoryEntities(ent);
                foreach (var item in items)
                {
                    Del(item);
                }
            }

            if (component.DeleteOrgans) // Frontier - Gib organs
            {
                if (TryComp<BodyComponent>(ent, out var body))
                {
                    var organs = _body.GetBodyOrganEntityComps<TransformComponent>((ent, body));
                    foreach (var organ in organs)
                    {
                        Del(organ.Owner);
                    }
                }
            } // Frontier

            if (component.Gib)
                _body.GibBody(ent, true);
            args.Handled = true;
        }
        // End Frontier
*/