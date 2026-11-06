using Content.Shared._NF.Manufacturing.Components;
using Content.Shared.Containers.ItemSlots;

namespace Content.Shared._NF.Manufacturing.EntitySystems;

/// <summary>
/// Consumes large quantities of power, scales excessive overage down to reasonable values.
/// Spawns entities when thresholds reached.
/// </summary>
public abstract partial class SharedEntitySpawnPowerConsumerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EntitySpawnPowerConsumerComponent, ItemSlotInsertAttemptEvent>(OnItemSlotInsertAttempt);
    }

    private void OnItemSlotInsertAttempt(Entity<EntitySpawnPowerConsumerComponent> ent, ref ItemSlotInsertAttemptEvent args)
    {
        if (args.User != null)
            args.Cancelled = true;
    }
}
